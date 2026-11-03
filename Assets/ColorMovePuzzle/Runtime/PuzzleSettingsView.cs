using System;
using TMPro;
using UnityEngine;

namespace ColorMovePuzzle
{
    public sealed class PuzzleSettingsView : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Slider bgm;
        [SerializeField] UnityEngine.UI.Slider sfx;
        [SerializeField] TMP_Text bgmValue;
        [SerializeField] TMP_Text sfxValue;
        [SerializeField] TMP_Text storageStatus;
        [SerializeField] UnityEngine.UI.Button closeButton;
        Action<float, float> changed;
        Action close;
        public UnityEngine.UI.Slider BgmSlider => bgm;
        public UnityEngine.UI.Slider SfxSlider => sfx;
        public UnityEngine.UI.Button CloseButton => closeButton;
        public void Configure(UnityEngine.UI.Slider music, UnityEngine.UI.Slider sound, TMP_Text musicText,
            TMP_Text soundText, TMP_Text storage, UnityEngine.UI.Button done)
        { bgm = music; sfx = sound; bgmValue = musicText; sfxValue = soundText; storageStatus = storage; closeButton = done; }
        public void Initialize(Action<float, float> onChanged, Action onClose)
        {
            changed = onChanged; close = onClose;
            bgm.onValueChanged.AddListener(Changed);
            sfx.onValueChanged.AddListener(Changed);
            closeButton.onClick.AddListener(Close);
        }
        public void Refresh(PuzzleMenuProfile profile, string error)
        {
            bgm.SetValueWithoutNotify(profile.BgmVolume);
            sfx.SetValueWithoutNotify(profile.SfxVolume);
            UpdateLabels();
            SetStorageError(error);
        }
        public void SetStorageError(string error)
            => storageStatus.text = string.IsNullOrEmpty(error) ? "YOUR PREFERENCES ARE SAVED AUTOMATICALLY" : "CHECK DEVICE STORAGE TO SAVE YOUR PREFERENCES";
        void UpdateLabels() { bgmValue.text = $"{Mathf.RoundToInt(bgm.value * 100)}%"; sfxValue.text = $"{Mathf.RoundToInt(sfx.value * 100)}%"; }
        void Changed(float value) { UpdateLabels(); changed?.Invoke(bgm.value, sfx.value); }
        void Close() => close?.Invoke();
        void OnDestroy()
        {
            if (bgm) bgm.onValueChanged.RemoveListener(Changed);
            if (sfx) sfx.onValueChanged.RemoveListener(Changed);
            if (closeButton) closeButton.onClick.RemoveListener(Close);
        }
    }
}
