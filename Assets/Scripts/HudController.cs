using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ActorFramework;
using DemoCollection;
using JetBrains.Annotations;
using UnityEngine;
using Noesis;

namespace DemoCollection
{
    public class HudController : MonoBehaviour, INotifyPropertyChanged
    {
        public static HudController Instance { get; private set; }

        private MainViewModel _mainViewModel;

        private void Awake()
        {
            Instance = this;
            _mainViewModel = new MainViewModel();
        }

        private void Start()
        {
            var view = GetComponent<NoesisView>();
            view.Content.DataContext = _mainViewModel;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
