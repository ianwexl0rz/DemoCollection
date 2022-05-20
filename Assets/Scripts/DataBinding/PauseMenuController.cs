using UnityEngine;

namespace DemoCollection.DataBinding
{
    [CreateAssetMenu (menuName = "Noesis/Pause Menu Controller", fileName = "Pause Menu Controller")]
    public class PauseMenuController : ObservableScriptableObject
    {
        [SerializeField] private GameSettings gameSettings;

        public bool InvertX
        {
            get => gameSettings.InvertX;
            set => SetProperty(ref gameSettings.InvertX, value);
        }
        
        public bool InvertY
        {
            get => gameSettings.InvertY;
            set => SetProperty(ref gameSettings.InvertY, value);
        }

        public DelegateCommand SetLookInvertX { get; private set; }
        public DelegateCommand SetLookInvertY { get; private set; }
        
        public void Init()
        {
            SetLookInvertX = new DelegateCommand(OnSetLookInvertX);
            SetLookInvertY = new DelegateCommand(OnSetLookInvertY);
        }
        
        public void OnSetLookInvertX(object obj) => InvertX = !InvertX;

        public void OnSetLookInvertY(object obj) => InvertY = !InvertY;
    }
}