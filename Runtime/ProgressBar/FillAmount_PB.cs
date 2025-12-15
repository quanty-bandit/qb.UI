using TriInspector;
using UnityEngine;
using UnityEngine.UI;
namespace qb.UI
{
    [AddComponentMenu("qb/UI/Progress/FillAmount_PB")]
    public class FillAmount_PB : ProgressBar
    {
        [OnValueChanged(nameof(UpdateUI))]
        [SerializeField, Required]
        Image target;
        protected override void UpdateUI()
        {
            if(target)
                target.fillAmount = Value;
        }
    }
}
