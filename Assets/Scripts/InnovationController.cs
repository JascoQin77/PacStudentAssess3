using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Level 2's signature mechanic: a limited pulse that briefly freezes the
// ghosts, giving the player a deliberate escape window when routes converge.
public class InnovationController : MonoBehaviour
{
    [SerializeField] float pulseDuration = 2.2f;
    [SerializeField] float pulseCooldown = 7.5f;

    PacStudentGameController game;
    PacStudentController player;
    GhostStateController[] ghosts;
    Text abilityText;
    Text statusText;
    LineRenderer pulseRing;
    float pulseRemaining;
    float cooldownRemaining;
    bool wasPlaying;
    bool pulseActive;

    public bool IsPulseActive => pulseActive;
    public float CooldownRemaining => cooldownRemaining;

    void Awake()
    {
        game = FindFirstObjectByType<PacStudentGameController>();
        player = FindFirstObjectByType<PacStudentController>();
        ghosts = FindObjectsByType<GhostStateController>(FindObjectsSortMode.None);
        CreateInterface();
        CreatePulseRing();
        SetInnovationTitle();
    }

    void Update()
    {
        if (game == null || player == null) return;

        bool playing = game.IsPlaying;
        if (!playing)
        {
            // The round controller owns countdown/death freezes. Do not let a
            // pulse completion undo one of those global freezes.
            if (wasPlaying && game.State == PacStudentGameController.RoundState.Dying)
                EndPulse(false);
            SetInnovationTitle();
            UpdateInterface();
            wasPlaying = false;
            return;
        }

        if (!wasPlaying)
        {
            cooldownRemaining = 0;
            pulseRemaining = 0;
            pulseActive = false;
        }
        wasPlaying = true;
        cooldownRemaining = Mathf.Max(0, cooldownRemaining - Time.deltaTime);
        if (pulseActive)
        {
            pulseRemaining -= Time.deltaTime;
            AnimatePulseRing();
            if (pulseRemaining <= 0) EndPulse(true);
        }
        if (Input.GetKeyDown(KeyCode.Space) && !pulseActive && cooldownRemaining <= 0)
            ActivatePulse();
        UpdateInterface();
    }

    void ActivatePulse()
    {
        pulseActive = true;
        pulseRemaining = pulseDuration;
        cooldownRemaining = pulseCooldown;
        foreach (var ghost in ghosts)
            if (ghost != null && ghost.State != GhostStateController.GhostState.Dead)
                ghost.SetFrozen(true);
        if (pulseRing != null) pulseRing.gameObject.SetActive(true);
    }

    void EndPulse(bool resumeGhosts)
    {
        pulseActive = false;
        pulseRemaining = 0;
        if (pulseRing != null) pulseRing.gameObject.SetActive(false);
        if (!resumeGhosts || game == null || !game.IsPlaying) return;
        foreach (var ghost in ghosts)
            if (ghost != null && ghost.State != GhostStateController.GhostState.Dead)
                ghost.SetFrozen(false);
    }

    void CreateInterface()
    {
        var hud = GameObject.Find("HUD");
        if (hud == null) return;
        var panelObject = new GameObject("Pulse Shift Panel", typeof(RectTransform), typeof(Image));
        var panel = panelObject.GetComponent<RectTransform>();
        panel.SetParent(hud.transform, false);
        panel.anchorMin = new Vector2(.02f, .91f);
        panel.anchorMax = new Vector2(.29f, .988f);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(.025f, .09f, .14f, .94f);

        abilityText = CreateLabel(panel, "Pulse Shift", 17, FontStyle.Bold, new Color(.42f, .94f, .86f));
        var abilityRect = abilityText.rectTransform;
        abilityRect.anchorMin = new Vector2(.06f, .48f);
        abilityRect.anchorMax = new Vector2(.94f, .95f);
        abilityRect.offsetMin = abilityRect.offsetMax = Vector2.zero;

        statusText = CreateLabel(panel, "SPACE  /  READY", 13, FontStyle.Normal, new Color(.96f, .94f, .86f));
        var statusRect = statusText.rectTransform;
        statusRect.anchorMin = new Vector2(.06f, .06f);
        statusRect.anchorMax = new Vector2(.94f, .48f);
        statusRect.offsetMin = statusRect.offsetMax = Vector2.zero;
    }

    Text CreateLabel(RectTransform parent, string wording, int size, FontStyle style, Color color)
    {
        var labelObject = new GameObject(wording, typeof(RectTransform), typeof(Text));
        var label = labelObject.GetComponent<Text>();
        label.transform.SetParent(parent, false);
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = wording;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = Mathf.Max(10, size - 4);
        label.resizeTextMaxSize = size;
        return label;
    }

    void UpdateInterface()
    {
        if (abilityText == null || statusText == null) return;
        abilityText.text = pulseActive ? "PULSE SHIFT  //  ACTIVE" : "PULSE SHIFT";
        if (pulseActive)
            statusText.text = "GHOSTS FROZEN   " + pulseRemaining.ToString("0.0") + "s";
        else if (cooldownRemaining > 0)
            statusText.text = "RECHARGING   " + cooldownRemaining.ToString("0.0") + "s";
        else
            statusText.text = "SPACE  /  READY";
    }

    void SetInnovationTitle()
    {
        var levelLabel = GameObject.Find("Level Name");
        var label = levelLabel == null ? null : levelLabel.GetComponent<Text>();
        if (label != null) label.text = "LEVEL 2 // PULSE SHIFT";
    }

    void CreatePulseRing()
    {
        var ringObject = new GameObject("Pulse Shift Ring");
        pulseRing = ringObject.AddComponent<LineRenderer>();
        pulseRing.positionCount = 48;
        pulseRing.loop = true;
        pulseRing.useWorldSpace = true;
        pulseRing.widthMultiplier = .065f;
        pulseRing.startColor = new Color(.42f, .94f, .86f, .9f);
        pulseRing.endColor = new Color(.42f, .94f, .86f, .2f);
        pulseRing.material = new Material(Shader.Find("Sprites/Default"));
        pulseRing.sortingOrder = 80;
        ringObject.SetActive(false);
    }

    void AnimatePulseRing()
    {
        if (pulseRing == null || player == null) return;
        float progress = 1f - Mathf.Clamp01(pulseRemaining / pulseDuration);
        float radius = Mathf.Lerp(.35f, 4.5f, progress);
        var center = player.transform.position;
        for (int i = 0; i < pulseRing.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / pulseRing.positionCount;
            pulseRing.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius, -.15f));
        }
        float alpha = Mathf.Lerp(.85f, .15f, progress);
        pulseRing.startColor = new Color(.42f, .94f, .86f, alpha);
        pulseRing.endColor = new Color(.42f, .94f, .86f, alpha * .35f);
    }

    void OnDestroy()
    {
        if (pulseRing != null && pulseRing.material != null)
            Destroy(pulseRing.material);
    }
}
