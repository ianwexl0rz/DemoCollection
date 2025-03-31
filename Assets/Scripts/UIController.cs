using System;
using System.Collections.Generic;
using System.Linq;
using DemoCollection.DataBinding;
using UnityEngine;

namespace DemoCollection
{
    public enum ViewState
    {
        HUD,
        Paused
    }
    
    public class UIController : ObservableMonobehaviour
    {
        private static UIController _instance;
        public static UIController Instance => _instance;

        [SerializeField] private PlayerController playerController = null;
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private GameObject pauseOverlay;

        [Header("HUD")]
        [SerializeField] private RectTransform hudOverlay = null;
        [SerializeField] private RectTransform targetIndicator;
        [SerializeField] private ResourceBar playerHealth = null;
        [SerializeField] private ResourceBar playerStamina = null;
        [SerializeField] private Transform enemyHealthContainer = null;
        [SerializeField] private ResourceBar enemyHealthPrefab = null;

        public void RegisterPlayer(Actor actor)
        {
            playerHealth.RegisterResource(actor.Health);
            playerStamina.RegisterResource(actor.Stamina);
        }
        
        public void UnregisterPlayer(Actor actor)
        {
            playerHealth.UnregisterResource(actor.Health);
            playerStamina.UnregisterResource(actor.Stamina);
        }
        
        private ViewState _state;
        public ViewState State {get => _state; private set => SetProperty(ref _state, value);}

        private Dictionary<Trackable, ResourceBar> _npcHealthBarMap = new Dictionary<Trackable, ResourceBar>();

        private void AddNpcHealthBar(Trackable trackable)
        {
            var healthBar = Instantiate(enemyHealthPrefab, enemyHealthContainer);
            healthBar.RegisterResource(trackable.Owner.Health);
            _npcHealthBarMap.Add(trackable, healthBar);
        }

        private void RemoveNpcHealthBar(Trackable trackable)
        {
            Destroy(_npcHealthBarMap[trackable].gameObject);
            _npcHealthBarMap.Remove(trackable);
        }
        
        public void UpdateHud()
        {
            if (State == ViewState.HUD)
            {
                bool hasTarget = playerController.TrackedTarget;
                targetIndicator.gameObject.SetActive(hasTarget);
                
                if (hasTarget)
                {
                    var trackable = playerController.TrackedTarget;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(hudOverlay, trackable.ScreenPos, null, out Vector2 localPoint);
                    targetIndicator.anchoredPosition = localPoint;

                    if (!_npcHealthBarMap.ContainsKey(trackable))
                    {
                        AddNpcHealthBar(trackable);
                    }
                }

                foreach (var trackable in playerController.RecentlyHit.Keys)
                {
                    if (!_npcHealthBarMap.ContainsKey(trackable))
                    {
                        AddNpcHealthBar(trackable);
                    }
                }
                
                List<Trackable> toRemove = new List<Trackable>();
                foreach (var trackable in _npcHealthBarMap.Keys)
                {
                    var healthBar = _npcHealthBarMap[trackable];
                    if (playerController.RecentlyHit.ContainsKey(trackable) || playerController.TrackedTarget == trackable)
                    {
                        // Update position
                        var rectTransform = healthBar.GetComponent<RectTransform>();
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(hudOverlay, trackable.ScreenPos, null, out Vector2 localPoint);
                        rectTransform.anchoredPosition = localPoint + Vector2.up * 100.0f;
                    }
                    else
                    {
                        // Mark for removal
                        healthBar.UnregisterResource(trackable.Owner.Health);
                        toRemove.Add(trackable);
                    }
                }

                foreach (var trackable in toRemove)
                {
                    RemoveNpcHealthBar(trackable);
                }
            }
            else
            {
                targetIndicator.gameObject.SetActive(false);
            }
        }

        public void Init()
        {
            playerController.OnPossessActor += RegisterPlayer;
            playerController.OnReleaseActor += UnregisterPlayer;
            
            _instance = this;
            _instance.OnHud();
        }

        public static void SetActiveView(ViewState state)
        {
            _instance.SetActiveViewInternal(state);
        }
        
        private void OnHud()
        {
            pauseOverlay.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            State = ViewState.HUD;
        }

        private void OnPause()
        {
            pauseOverlay.SetActive(true);
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            State = ViewState.Paused;
        }

        private void SetActiveViewInternal(ViewState state)
        {
            if (State == state)
                return;

            if (state == ViewState.HUD)
                OnHud();

            else if (state == ViewState.Paused)
                OnPause();
        }
    }
}
