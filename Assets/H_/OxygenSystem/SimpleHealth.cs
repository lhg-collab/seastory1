using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SimpleHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] float maxHealth = 100f;
    public float CurrentHealth { get; private set; }

    [Header("UI (자동 바인딩)")]
    [SerializeField] Image healthFill;     // Filled
    [SerializeField] Text healthInnerText; // ▶ 게이지 안쪽 Text(자식 Text 자동 탐색)
    [SerializeField] Gradient healthColor;

    [Header("저체력 붉은 화면 깜빡임")]
    [SerializeField] Image damageFlashOverlay;   // 풀스크린 빨간 Image
    [SerializeField] float lowHealthThreshold = 0.5f; // 50% 미만
    [SerializeField] float flashMinAlpha = 0.0f;
    [SerializeField] float flashMaxAlpha = 0.35f;
    [SerializeField] float flashSpeed = 3.5f;

    // 자동 바인딩 후보
    static readonly string[] HealthFillNames = { "HealthFill", "HPFill", "Health", "HP" };
    static readonly string[] HealthLabelNames = { "HealthLabel", "HPLabel" };

    void Awake()
    {
        CurrentHealth = maxHealth;

        if (healthColor == null || healthColor.colorKeys.Length == 0)
        {
            var g = new Gradient();
            g.colorKeys = new[]
            {
                new GradientColorKey(new Color(0.2f, 0.9f, 0.2f), 0f),
                new GradientColorKey(Color.yellow, 0.5f),
                new GradientColorKey(new Color(1f, 0.25f, 0.25f), 1f),
            };
            g.alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) };
            healthColor = g;
        }

        TryAutoBindUI();
        UpdateUI(force: true);
    }

    void Update()
    {
        UpdateDamageFlash();
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        UpdateUI(force: true);
        // TODO: 0일 때 사망 로직 필요하면 추가
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        UpdateUI(force: true);
    }

    public float HealthRatio() => maxHealth <= 0f ? 0f : CurrentHealth / maxHealth;

    void UpdateUI(bool force = false)
    {
        float r = HealthRatio();

        if (healthFill)
        {
            if (healthFill.type != Image.Type.Filled) healthFill.type = Image.Type.Filled;
            healthFill.fillAmount = r;
            healthFill.color = healthColor.Evaluate(r);
            healthFill.raycastTarget = false;

            // ▶ Fill 자식 Text 자동
            if (!healthInnerText || force)
                healthInnerText = healthFill.GetComponentsInChildren<Text>(true).FirstOrDefault();
        }

        if (healthInnerText)
        {
            int cur = Mathf.RoundToInt(CurrentHealth);
            int max = Mathf.RoundToInt(maxHealth);
            healthInnerText.text = $"{cur}/{max}";
        }
    }

    void UpdateDamageFlash()
    {
        if (!damageFlashOverlay) return;

        float r = HealthRatio();
        if (r < lowHealthThreshold && r > 0f)
        {
            float t = (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f; // 0~1
            float a = Mathf.Lerp(flashMinAlpha, flashMaxAlpha, t);
            var c = damageFlashOverlay.color; c.a = a; damageFlashOverlay.color = c;
            damageFlashOverlay.raycastTarget = false;
            if (!damageFlashOverlay.gameObject.activeSelf) damageFlashOverlay.gameObject.SetActive(true);
        }
        else
        {
            var c = damageFlashOverlay.color; c.a = 0f; damageFlashOverlay.color = c;
            if (damageFlashOverlay.gameObject.activeSelf) damageFlashOverlay.gameObject.SetActive(false);
        }
    }

    void TryAutoBindUI()
    {
        // HealthFill
        if (!healthFill)
            healthFill = FindImageByNames(HealthFillNames)
                      ?? FindUnderLabel("HealthLabel", "Fill");

        // 안쪽 텍스트: Fill 자식 Text 우선
        if (!healthInnerText && healthFill)
            healthInnerText = healthFill.GetComponentsInChildren<Text>(true).FirstOrDefault();

        // 붉은 오버레이 자동
        if (!damageFlashOverlay)
        {
            var names = new[] { "DamageOverlay", "RedFlash", "LowHPOverlay" };
            damageFlashOverlay = FindImageByNames(names);
        }
    }

    Image FindImageByNames(string[] names)
    {
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go)
            {
                var img = go.GetComponent<Image>();
                if (img) return img;
            }
        }
        return null;
    }

    Image FindUnderLabel(string labelName, string contains = "Fill")
    {
        var label = GameObject.Find(labelName);
        if (!label) return null;
        return label.GetComponentsInChildren<Image>(true)
                    .FirstOrDefault(i => i.name.IndexOf(contains, System.StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
