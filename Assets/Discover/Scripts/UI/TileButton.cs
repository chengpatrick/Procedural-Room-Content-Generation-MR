// Copyright (c) Meta Platforms, Inc. and affiliates.

using Discover.Haptics;
using Discover.Utils;
using Oculus.Interaction.Input;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Discover.UI
{
    public class TileButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
    {
        public UnityEvent<Handedness> OnClick;

        [SerializeField]
        private Sprite m_sourceImage;
        public Sprite SourceImage
        {
            get => m_sourceImage;
            set
            {
                m_sourceImage = value;
                m_imageComponent.sprite = m_sourceImage;
            }
        }

        [SerializeField]
        private string m_title;
        public string Title
        {
            get => m_title;
            set
            {
                m_title = value;
                m_textComponent.text = m_title;
            }
        }

        [SerializeField]
        private Image m_imageComponent;
        [SerializeField]
        private TextMeshProUGUI m_textComponent;

        [SerializeField]
        private VibrationForce m_hapticsHoverForce = VibrationForce.LIGHT;
        [SerializeField]
        private VibrationForce m_hapticsPressForce = VibrationForce.HARD;
        [SerializeField]
        private float m_hapticsDuration = 0.05f;

        private void Awake()
        {
            FindDependencies();

            Assert.IsNotNull(m_textComponent, $"{nameof(m_textComponent)} cannot be null.");
            Assert.IsNotNull(m_imageComponent, $"{nameof(m_imageComponent)} cannot be null.");

            SourceImage = m_sourceImage;
            Title = m_title;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"tile button with {m_title} pressed {eventData.pointerId}");
            var handedness = ControllerUtils.GetHandFromPointerData(eventData);
            var controller = ControllerUtils.GetControllerFromHandedness(handedness);
            HapticsManager.Instance.VibrateForDuration(m_hapticsPressForce, m_hapticsDuration, controller);
            Click(handedness);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var controller = ControllerUtils.GetControllerFromPointerData(eventData);
            HapticsManager.Instance.VibrateForDuration(m_hapticsHoverForce, m_hapticsDuration, controller);
        }

        public void OnPointerExit(PointerEventData eventData) { }

        private void Click(Handedness handedness)
        {
            OnClick?.Invoke(handedness);
        }

        private void FindDependencies()
        {
            if (m_textComponent == null)
            {
                m_textComponent = GetComponentInChildren<TextMeshProUGUI>();
            }
            if (m_imageComponent == null)
            {
                var imageTransform = transform.FindChildRecursive("Image");
                if (imageTransform != null)
                {
                    m_imageComponent = imageTransform.gameObject.GetComponentInChildren<Image>();
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            FindDependencies();
        }
#endif
    }
}
