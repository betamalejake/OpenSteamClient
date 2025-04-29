using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Media;
using OpenSteamworks.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenSteamworks.Data.Structs;

namespace OpenSteamClient.ViewModels.Library;

public abstract partial class Node : AvaloniaCommon.ViewModelBase, IComparable<Node>
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private IBrush icon = Brushes.Transparent;

    [ObservableProperty]
    private IBrush statusIcon = Brushes.Transparent;

    //TODO: This is really not ideal as light mode users will have light on light text
    [ObservableProperty]
    private IBrush foreground = Brushes.White;

    [ObservableProperty]
    private bool hasIcon;

    [ObservableProperty]
    private bool isApp;

    [ObservableProperty]
    private bool isExpanded;

    public ObservableCollectionEx<Node> Children { get; protected set; } = new();
    public CGameID GameID { get; protected set; }

    public Node()
    {
        Children.CollectionChanged += OnChildrenChanged;
    }

    protected void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        base.OnPropertyChanged(nameof(Name));
    }

    public int CompareTo(Node? other)
    {
        return string.Compare(this.GetSortableName(), other?.GetSortableName());
    }

    public string GetSortableName() {
        if (!this.IsApp || this is not LibraryAppViewModel libraryAppViewModel) {
            return this.Name;
        }

        var name = libraryAppViewModel.App.Name;
        if (name.StartsWith("A ")) {
            return name.Replace("A ", "");
        } else if (name.StartsWith("The ")) {
            return name.Replace("The ", "");
        }

        return name;
    }
}
