using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UI;
using TMPro;

public class MagicPickManager : MonoBehaviour
{
    public List<CardController> cards;

    [Header("Position References (Size 3)")]
    public Transform[] initialPositions;
    public Transform[] shufflePositions;
    public Transform[] revealPositions;

    [Header("UI References")]
    public GameObject collectButton;
    public GameObject hud;
    public TextMeshProUGUI coinValueText;
    public TextMeshProUGUI pickCardText;

    [Header("Coin Collection VFX")]
    public GameObject coinPrefabForAnimation;
    public Transform coinTargetTransform;
    public int coinsToAnimate = 15;
    public float startBurstRange = 50f;

    [HideInInspector]
    public float CoinFlightDuration = 0.2f;

    [HideInInspector]
    public float CoinSpawnInterval = 0.02f;

    [HideInInspector]
    public float HudDownDuration = 1f;

    [HideInInspector]
    public float HudUpDuration = 1f;

    [HideInInspector]
    public float InitialVisualDelay = 0.1f;

    [Header("VFX References")]
    public Image dimmerOverlay;
    public GameObject sunburstRaysVFX;
    public GameObject shurikenVFX;

    [Header("Audio")]
    public AudioClip shuffleSFX;
    public AudioClip revealSFX;
    public AudioClip coinSFX;
    public AudioClip drumRollSFX;

    private AudioSource audioSource;
    private CardController selectedCard = null;
    private int currentCoinTotal = 0;
    private int rewardCollected = 0;
    private RectTransform canvasRect;

    async void Start()
    {
        if (cards.Count != initialPositions.Length || cards.Count != shufflePositions.Length || cards.Count != revealPositions.Length)
        {
            Debug.LogError("All position arrays must be the same size as the card list (3).");
            return;
        }

        if (transform.parent != null)
        {
            Canvas rootCanvas = transform.parent.GetComponentInParent<Canvas>();
            if (rootCanvas != null)
            {
                canvasRect = rootCanvas.GetComponent<RectTransform>();
            }
        }

        if (canvasRect == null)
        {
            Debug.LogError("MagicPickManager must be parented under a Canvas or a parent of a Canvas for UI coordinate conversion to work.");
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.position = initialPositions[i].position;
            cards[i].GetComponent<Button>().interactable = false;
        }

        if (coinValueText != null) coinValueText.text = currentCoinTotal.ToString();

        if (hud != null)
        {
            Vector3 finalLocalPos = hud.transform.localPosition;
            hud.transform.localPosition = new Vector3(finalLocalPos.x, finalLocalPos.y + 300, finalLocalPos.z);
            hud.SetActive(false);
        }

        if (pickCardText != null)
        {
            pickCardText.gameObject.SetActive(false);
        }

        if (dimmerOverlay != null)
        {
            Color c = dimmerOverlay.color;
            c.a = 0f;
            dimmerOverlay.color = c;
            dimmerOverlay.raycastTarget = false;
        }

        if (sunburstRaysVFX != null)
        {
            sunburstRaysVFX.SetActive(false);
            sunburstRaysVFX.transform.SetParent(this.transform.parent, true);
        }

        if (shurikenVFX != null)
        {
            shurikenVFX.SetActive(false);
            shurikenVFX.transform.SetParent(this.transform.parent, true);
        }

        await StartSequence();
    }

    void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    async Task StartSequence()
    {
        foreach (var card in cards) card.ShowFront();
        await Task.Delay(1000);

        List<Tween> flipTweens = new List<Tween>();
        foreach (var card in cards) flipTweens.Add(card.Flip(false));
        await Task.WhenAll(flipTweens.ConvertAll(t => t.AsyncWaitForCompletion()));

        await ShuffleCards();

        float snapDuration = 0.3f;
        List<Tween> snapTweens = new List<Tween>();
        Vector3[] finalRotations = { new Vector3(0, 0, 0), new Vector3(0, 0, 5), new Vector3(0, 0, -5) };

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.localScale = Vector3.one;
            cards[i].transform.SetAsFirstSibling();

            Tween moveTween = cards[i].transform.DOMove(shufflePositions[i].position, snapDuration).SetEase(Ease.OutQuad);
            Tween rotateTween = cards[i].transform.DORotate(finalRotations[i], snapDuration).SetEase(Ease.OutQuad);

            snapTweens.Add(moveTween);
            snapTweens.Add(rotateTween);
        }
        await Task.WhenAll(snapTweens.ConvertAll(t => t.AsyncWaitForCompletion()));

        float wiggleDelay = 0.15f;

        for (int i = 0; i < cards.Count; i++)
        {
            await Task.Delay((int)(wiggleDelay * 1000));

            cards[i].Wiggle();
            cards[i].GetComponent<Button>().interactable = true;
        }

        if (pickCardText != null)
        {
            pickCardText.gameObject.SetActive(true);
        }
    }

    async Task ShuffleCards()
    {
        if (shuffleSFX != null && audioSource != null)
        {
            audioSource.clip = shuffleSFX;
            audioSource.loop = false;
            audioSource.Play();
        }

        float swapDuration = 0.2f;
        int totalSwaps = 12;

        for (int step = 0; step < totalSwaps; step++)
        {
            int indexA = Random.Range(0, cards.Count);
            int indexB = Random.Range(0, cards.Count);
            while (indexA == indexB) indexB = Random.Range(0, cards.Count);

            CardController cardA = cards[indexA];
            CardController cardB = cards[indexB];

            Vector3 posA = cardA.transform.position;
            Vector3 posB = cardB.transform.position;

            Sequence seqA = DOTween.Sequence();
            seqA.AppendCallback(() => cardA.transform.SetAsLastSibling())
                .Append(cardA.transform.DOMove(posB, swapDuration).SetEase(Ease.InOutSine))
                .Join(cardA.transform.DOScale(1.1f, swapDuration * 0.5f).SetEase(Ease.OutQuad))
                .Append(cardA.transform.DOScale(1.0f, swapDuration * 0.5f).SetEase(Ease.InQuad));

            Sequence seqB = DOTween.Sequence();
            seqB.AppendCallback(() => cardB.transform.SetAsLastSibling())
                .Append(cardB.transform.DOMove(posA, swapDuration).SetEase(Ease.InOutSine))
                .Join(cardB.transform.DOScale(1.1f, swapDuration * 0.5f).SetEase(Ease.OutQuad))
                .Append(cardB.transform.DOScale(1.0f, swapDuration * 0.5f).SetEase(Ease.InQuad));

            CardController temp = cards[indexA];
            cards[indexA] = cards[indexB];
            cards[indexB] = temp;

            await Task.WhenAll(seqA.AsyncWaitForCompletion(), seqB.AsyncWaitForCompletion());
        }

        if (audioSource != null && audioSource.isPlaying && audioSource.clip == shuffleSFX)
        {
            audioSource.Stop();
        }
    }

    public void SelectCard(CardController card)
    {
        DOTween.KillAll(true);

        if (pickCardText != null)
        {
            pickCardText.gameObject.SetActive(false);
        }

        foreach (var c in cards)
        {
            c.GetComponent<Button>().interactable = false;
            c.KillWiggle();
        }

        if (dimmerOverlay != null)
        {
            dimmerOverlay.transform.SetAsLastSibling();
            dimmerOverlay.DOFade(0.6f, 0.2f).SetEase(Ease.OutQuad);
        }

        if (sunburstRaysVFX != null)
        {
            sunburstRaysVFX.transform.SetParent(card.transform, false);
            sunburstRaysVFX.transform.localPosition = Vector3.zero;
            sunburstRaysVFX.SetActive(true);
        }

        if (shurikenVFX != null)
        {
            shurikenVFX.transform.SetParent(card.transform, false);
            shurikenVFX.transform.localPosition = Vector3.zero;
            shurikenVFX.transform.localScale = Vector3.one;
        }

        card.transform.SetAsLastSibling();
        card.transform.rotation = Quaternion.Euler(Vector3.zero);

        selectedCard = card;
        rewardCollected = card.rewardValue;

        RevealAllCards();
    }

    void RevealAllCards()
    {
        Sequence fullRevealSequence = DOTween.Sequence();
        int selectedIndex = cards.IndexOf(selectedCard);

        Sequence selectedRevealSequence = DOTween.Sequence();

        float targetScale = 1.2f;
        float scaleDuration = 0.2f;
        float grandRotateDuration = 4.0f;
        float aggressiveSpinDegrees = 1440f;
        float finalFlipSpeed = 0.2f;

        selectedRevealSequence.Append(selectedCard.transform.DOScale(Vector3.one * targetScale, scaleDuration).SetEase(Ease.OutQuad));

        selectedRevealSequence.AppendCallback(() => PlaySFX(drumRollSFX));

        selectedRevealSequence.Append(
            selectedCard.transform.DORotate(new Vector3(0, aggressiveSpinDegrees, 0), grandRotateDuration, RotateMode.FastBeyond360).SetEase(Ease.InOutSine)
        );

        selectedRevealSequence.AppendCallback(() => selectedCard.transform.rotation = Quaternion.Euler(0, 0, 0));

        selectedRevealSequence.AppendCallback(() => {
            if (sunburstRaysVFX != null)
            {
                sunburstRaysVFX.transform.SetParent(this.transform.parent, true);
                sunburstRaysVFX.SetActive(false);
            }
        });

        selectedRevealSequence.AppendCallback(() => {
            if (shurikenVFX != null)
            {
                shurikenVFX.SetActive(true);
                ParticleSystem ps = shurikenVFX.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                }
            }
        });

        selectedRevealSequence.Append(selectedCard.transform.DORotate(new Vector3(0, 90, 0), finalFlipSpeed / 2f).SetEase(Ease.Linear));

        selectedRevealSequence.AppendCallback(() => {
            audioSource.Stop();
            selectedCard.ShowFront();
            PlaySFX(revealSFX);
        });

        selectedRevealSequence.Append(selectedCard.transform.DORotate(new Vector3(0, 180, 0), finalFlipSpeed / 2f).SetEase(Ease.Linear));

        selectedRevealSequence.AppendCallback(() => selectedCard.transform.rotation = Quaternion.Euler(0, 0, 0));

        selectedRevealSequence.Join(selectedCard.transform.DOScale(Vector3.one, finalFlipSpeed).SetEase(Ease.OutBack));

        fullRevealSequence.Append(selectedRevealSequence);

        for (int i = 0; i < cards.Count; i++)
        {
            if (i != selectedIndex)
            {
                fullRevealSequence.AppendInterval(0.2f);
                fullRevealSequence.Append(cards[i].Flip(true));
            }
        }

        fullRevealSequence.OnComplete(() =>
        {
            if (dimmerOverlay != null) dimmerOverlay.DOFade(0f, 0.3f);

            if (shurikenVFX != null)
            {
                shurikenVFX.transform.SetParent(this.transform.parent, true);
                shurikenVFX.SetActive(false);
            }

            if (dimmerOverlay != null)
            {
                selectedCard.transform.SetSiblingIndex(dimmerOverlay.transform.GetSiblingIndex());
            }

            Invoke("ShowCollectButton", 0.2f);
        });
    }

    void ShowCollectButton()
    {
        if (collectButton == null) return;

        collectButton.transform.localScale = Vector3.zero;
        collectButton.SetActive(true);
        collectButton.transform.DOScale(1, 0.3f).SetEase(Ease.OutBack);

        Button btn = collectButton.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(async () => await OnCollect());
        }
    }

    Tween ShowHUDTween()
    {
        if (hud == null) return null;

        hud.SetActive(true);

        Vector3 finalLocalPos = hud.transform.localPosition;
        finalLocalPos.y -= 300;

        return hud.transform.DOLocalMove(finalLocalPos, HudDownDuration).SetEase(Ease.OutQuad);
    }

    Vector2 WorldToCanvasLocalPosition(Vector3 worldPosition, RectTransform canvas)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas,
            screenPoint,
            Camera.main,
            out Vector2 localPoint))
        {
            return localPoint;
        }
        return Vector2.zero;
    }

    async Task AnimateCoinTransfer()
    {
        if (selectedCard == null || coinPrefabForAnimation == null || coinTargetTransform == null || canvasRect == null)
        {
            Debug.LogWarning("Missing references for coin transfer animation. Aborting.");
            return;
        }

        Vector2 startAnchorPos = WorldToCanvasLocalPosition(selectedCard.transform.position, canvasRect);
        Vector2 endAnchorPos = WorldToCanvasLocalPosition(coinTargetTransform.position, canvasRect);

        const float jumpHeight = 150f;
        List<Task> coinFlyTasks = new List<Task>();

        for (int i = 0; i < coinsToAnimate; i++)
        {
            Vector2 scatteredStart = startAnchorPos + new Vector2(
                Random.Range(-startBurstRange, startBurstRange),
                Random.Range(-startBurstRange, startBurstRange)
            );

            GameObject flyingCoinObject = Instantiate(coinPrefabForAnimation, canvasRect.transform);
            RectTransform flyingCoinRect = flyingCoinObject.GetComponent<RectTransform>();

            if (flyingCoinRect == null)
            {
                Debug.LogError("Coin Prefab must have a RectTransform (be a UI element).");
                Destroy(flyingCoinObject);
                continue;
            }

            flyingCoinRect.anchoredPosition = scatteredStart;
            flyingCoinObject.SetActive(true);

            flyingCoinObject.transform.SetAsLastSibling();

            Sequence coinAnimation = DOTween.Sequence();

            coinAnimation.Append(
                flyingCoinRect.DOJumpAnchorPos(
                    endAnchorPos,
                    jumpHeight,
                    1,
                    CoinFlightDuration
                ).SetEase(Ease.OutQuad)
            );

            float soundInsertionTime = Mathf.Max(0.0f, CoinFlightDuration - 0.1f);
            coinAnimation.Insert(soundInsertionTime, DOTween.Sequence().AppendCallback(() => {
                PlaySFX(coinSFX);
            }));

            coinAnimation.Join(
                flyingCoinObject.transform.DORotate(
                    new Vector3(0, 0, 360 * Random.Range(2, 5)),
                    CoinFlightDuration,
                    RotateMode.FastBeyond360
                )
            );

            coinAnimation.SetDelay(i * CoinSpawnInterval)
                .OnComplete(() =>
                {
                    Destroy(flyingCoinObject, 0.05f);
                });

            coinFlyTasks.Add(coinAnimation.AsyncWaitForCompletion());
        }

        await Task.WhenAll(coinFlyTasks);
    }

    Tween HideHUD()
    {
        if (hud == null) return null;

        Vector3 currentPos = hud.transform.localPosition;
        Vector3 endPos = new Vector3(currentPos.x, currentPos.y + 300, currentPos.z);

        return hud.transform.DOLocalMove(endPos, HudUpDuration).SetEase(Ease.InQuad)
                    .OnComplete(() => hud.SetActive(false));
    }

    public async Task OnCollect()
    {
        DOTween.Kill(collectButton.transform);
        collectButton.SetActive(false);

        if (hud == null || coinValueText == null)
        {
            Debug.LogError("Missing HUD/Text references. Falling back to simple count.");
            currentCoinTotal += rewardCollected;
            if (coinValueText != null) coinValueText.text = currentCoinTotal.ToString();
            return;
        }

        await ShowHUDTween().AsyncWaitForCompletion();

        await Task.Delay((int)(InitialVisualDelay * 1000));

        float totalCoinVisualDuration = CoinFlightDuration + (coinsToAnimate * CoinSpawnInterval);

        int startValue = currentCoinTotal;
        currentCoinTotal += rewardCollected;

        DOTween.To(() => startValue, x => coinValueText.text = x.ToString(), currentCoinTotal, totalCoinVisualDuration)
               .SetEase(Ease.Linear);

        await AnimateCoinTransfer();

        await HideHUD().AsyncWaitForCompletion();
    }
}