using System;
using System.ComponentModel;
using UnityEngine;

[Serializable]
public class ResourcePool
{
    [SerializeField] private int _current = 100;
    [SerializeField] private int _maximum = 100;

    public event PropertyChangedEventHandler PropertyChanged;

    public int Current
    {
        get => _current;
        set { if (_current != value) { _current = value; OnPropertyChanged("Current"); } }
    }

    public int Maximum
    {
        get => _maximum;
        set { if (_maximum != value) { _maximum = value; OnPropertyChanged("Maximum"); } }
    }

    public void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}