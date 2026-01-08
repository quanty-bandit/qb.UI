using TriInspector;
using UnityEngine;
using UnityEngine.UI;
namespace qb.UI
{
    /// <summary>
    /// A progress bar component that updates the fill amount of a target Unity UI Image based on the current value.
    /// </summary>
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
