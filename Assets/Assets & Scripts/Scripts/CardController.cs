using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CardController : MonoBehaviour
{
    public MagicPickManager Manager;
    public int rewardValue = 100;

    public Image front;
    public Image back;

    private Button cardButton;
    private const string WIGGLE_ID = "CardWiggle";

    void Awake()
    {
        cardButton = GetComponent<Button>();
        cardButton.onClick.AddListener(ClickHandler);
    }

    private void ClickHandler()
    {
        if (Manager != null)
        {
            Manager.SelectCard(this);
        }
    }

    public void ShowFront()
    {
        front.gameObject.SetActive(true);
        back.gameObject.SetActive(false);
    }

    public void ShowBack()
    {
        front.gameObject.SetActive(false);
        back.gameObject.SetActive(true);
    }

    public Tween Flip(bool showFrontAfter)
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DORotate(new Vector3(0, 90, 0), 0.25f))
           .AppendCallback(() =>
           {
               if (showFrontAfter) ShowFront();
               else ShowBack();
           })
           .Append(transform.DORotate(Vector3.zero, 0.25f));
        return seq;
    }

    public void Wiggle()
    {
        float currentZ = transform.localEulerAngles.z;
        if (currentZ > 180) currentZ -= 360;

        float wiggleRange = 2f;
        float targetZ1 = currentZ - wiggleRange;
        float targetZ2 = currentZ + wiggleRange;

        Sequence wiggleSeq = DOTween.Sequence();

        wiggleSeq.Append(transform.DORotate(new Vector3(0, 0, targetZ1), 0.3f))
                 .Append(transform.DORotate(new Vector3(0, 0, targetZ2), 0.3f))
                 .SetLoops(-1, LoopType.Yoyo)
                 .SetEase(Ease.InOutSine)
                 .SetId(WIGGLE_ID);
    }

    public void KillWiggle()
    {
        DOTween.Kill(WIGGLE_ID);
    }
}