using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class UIAmmoCounter : MonoBehaviour
{
    CanvasGroup canvasGroup;
    [SerializeField] TMP_Text lblCurrentWeapon, lblDebugAmmo;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();    
    }

    public void SetCurrentWeapon(string weaponName) => lblCurrentWeapon.text = weaponName;
    public void SetCurrentAmmo(int bullets, int mags) => lblDebugAmmo.text = $"{Mathf.Clamp(bullets, 0, 99):00}/{Mathf.Clamp(mags, 0, 99):00}";
    public void Toggle(bool toggle) => canvasGroup.alpha = toggle ? 1 : 0;
}
