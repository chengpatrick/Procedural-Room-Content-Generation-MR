// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Meta.Utilities;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace Discover.Networking
{
    [RequireComponent(typeof(Grabbable))]
    public class NetworkGrabbableObject : NetworkBehaviour
    {
        [AutoSet]
        [SerializeField] private Grabbable m_grabbable;

        public UnityEvent<bool> OnSelected;
        public UnityEvent<bool> OnUnselected;

        private void OnEnable()
        {
            m_grabbable.WhenPointerEventRaised += OnPointerEventRaised;
        }

        private void OnDisable()
        {
            m_grabbable.WhenPointerEventRaised -= OnPointerEventRaised;
        }

        private void OnPointerEventRaised(PointerEvent pointerEvent)
        {
            switch (pointerEvent.Type)
            {
                case PointerEventType.Select:
                    if (m_grabbable.SelectingPointsCount == 1)
                    {
                        TransferOwnershipToLocalPlayer();
                    }
                    OnSelected?.Invoke(HasStateAuthority);
                    break;
                case PointerEventType.Unselect:
                    OnUnselected?.Invoke(HasStateAuthority);
                    break;
                case PointerEventType.Hover:
                    break;
                case PointerEventType.Unhover:
                    break;
                case PointerEventType.Move:
                    // GetComponent<Rigidbody>().isKinematic = true;
                    break;
                case PointerEventType.Cancel:
                    GetComponent<Rigidbody>().isKinematic = false;
                    break;
                default:
                    break;
            }
        }

        private void TransferOwnershipToLocalPlayer()
        {
            if (!HasStateAuthority)
            {
                Object.RequestStateAuthority();
            }
        }
    }
}