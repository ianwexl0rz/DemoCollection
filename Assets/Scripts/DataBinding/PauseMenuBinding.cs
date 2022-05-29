using System;
using UnityEngine;

namespace DemoCollection.DataBinding
{
    [Serializable]
    public class PauseMenuBinding : ObservableObject
    {
        private GameSettings _gameSettings;

        public bool InvertX
        {
            get => _gameSettings.InvertX;
            set => SetProperty(ref _gameSettings.InvertX, value);
        }
        
        public bool InvertY
        {
            get => _gameSettings.InvertY;
            set => SetProperty(ref _gameSettings.InvertY, value);
        }

        public DelegateCommand SetLookInvertX { get; private set; }
        public DelegateCommand SetLookInvertY { get; private set; }
        
        public PauseMenuBinding(GameSettings gameSettings)
        {
            _gameSettings = gameSettings;
            SetLookInvertX = new DelegateCommand(OnSetLookInvertX);
            SetLookInvertY = new DelegateCommand(OnSetLookInvertY);
        }
        
        public void OnSetLookInvertX(object obj) => InvertX = !InvertX;

        public void OnSetLookInvertY(object obj) => InvertY = !InvertY;
    }
}