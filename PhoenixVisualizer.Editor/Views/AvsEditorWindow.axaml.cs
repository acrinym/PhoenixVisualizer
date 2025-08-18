using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Services;
using PhoenixVisualizer.Editor.ViewModels;
using System;
using System.Linq;
using Avalonia.Platform;

namespace PhoenixVisualizer.Editor.Views;

public partial class AvsEditorWindow : Window
{
    private AvsEditorViewModel? _viewModel;

    public AvsEditorWindow()
    {
        InitializeComponent();
        
        _viewModel = new AvsEditorViewModel();
        DataContext = _viewModel;
        
        // Wire up the preview canvas
        if (PreviewCanvas != null)
        {
            _viewModel.SetPreviewCanvas(PreviewCanvas);
        }
        
        // Set up drag and drop for effects
        SetupDragAndDrop();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        _viewModel = DataContext as AvsEditorViewModel;
        if (_viewModel != null)
        {
            // Set up double-click to add effects
            SetupEffectLibraryDoubleClick();
        }
    }

    private void SetupDragAndDrop()
    {
        // Make effect library items draggable
        var effectLibrary = this.FindControl<ListBox>("EffectLibrary");
        if (effectLibrary != null)
        {
            effectLibrary.AddHandler(PointerPressedEvent, OnEffectLibraryPointerPressed, RoutingStrategies.Tunnel);
        }

        // Make section areas droppable
        SetupSectionDropZones();
    }

    private void SetupEffectLibraryDoubleClick()
    {
        var effectLibrary = this.FindControl<ListBox>("EffectLibrary");
        if (effectLibrary != null)
        {
            effectLibrary.DoubleTapped += OnEffectLibraryDoubleTapped;
        }
    }

    private void SetupSectionDropZones()
    {
        // Find all section areas and make them droppable
        var sections = new[] { "InitSection", "BeatSection", "FrameSection", "PointSection" };
        
        foreach (var sectionName in sections)
        {
            var section = this.FindControl<Border>(sectionName);
            if (section != null)
            {
                section.AddHandler(DragDrop.DropEvent, OnSectionDrop, RoutingStrategies.Tunnel);
                section.AddHandler(DragDrop.DragOverEvent, OnSectionDragOver, RoutingStrategies.Tunnel);
            }
        }
    }

    private void OnEffectLibraryPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is ListBox listBox && e.GetCurrentPoint(listBox).Properties.IsLeftButtonPressed)
        {
            var point = e.GetPosition(listBox);
            var item = GetItemAtPoint(listBox, point);
            
            if (item is AvsEffect effect)
            {
                var data = new DataObject();
                data.Set("EffectId", effect.Id);
                data.Set("EffectSection", effect.Section);
                
                DragDrop.DoDragDrop(e, data, DragDropEffects.Copy);
            }
        }
    }

    private object? GetItemAtPoint(ListBox listBox, Point point)
    {
        // Find the item at the given point by checking each item's bounds
        for (int i = 0; i < listBox.ItemCount; i++)
        {
            var item = listBox.ContainerFromIndex(i);
            if (item is Control control)
            {
                var bounds = control.Bounds;
                if (bounds.Contains(point))
                {
                    return listBox.Items[i];
                }
            }
        }
        return null;
    }

    private void OnEffectLibraryDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is AvsEffect effect)
        {
            // Add the effect to the appropriate section
            if (_viewModel != null)
            {
                _viewModel.AddEffect(effect.Id);
            }
        }
    }

    private void OnEffectDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is AvsEffect effect)
        {
            // Add the effect to the appropriate section
            if (_viewModel != null)
            {
                _viewModel.AddEffect(effect.Id);
            }
        }
    }

    private void OnSectionDrop(object? sender, DragEventArgs e)
    {
        if (sender is Border section && e.Data.Contains("EffectId"))
        {
            var effectId = e.Data.Get("EffectId") as string;
            var sectionType = GetSectionTypeFromBorder(section);
            
            if (!string.IsNullOrEmpty(effectId) && sectionType.HasValue)
            {
                AddEffectToSection(effectId, sectionType.Value);
            }
        }
    }

    private void OnSectionDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("EffectId"))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private AvsSection? GetSectionTypeFromBorder(Border section)
    {
        // Determine section type based on the border's name or content
        var sectionName = section.Name;
        
        return sectionName switch
        {
            "InitSection" => AvsSection.Init,
            "BeatSection" => AvsSection.Beat,
            "FrameSection" => AvsSection.Frame,
            "PointSection" => AvsSection.Point,
            _ => null
        };
    }

    private void AddEffectToSection(string effectId, AvsSection section)
    {
        if (_viewModel == null) return;

        try
        {
            _viewModel.AddEffect(effectId);
        }
        catch (Exception ex)
        {
            // TODO: Show error message to user
            System.Diagnostics.Debug.WriteLine($"Failed to add effect to section: {ex.Message}");
        }
    }

    // Context menu handlers for effects
    private void OnEffectContextMenu(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is AvsEffect effect)
        {
            var action = menuItem.Name;
            
            switch (action)
            {
                case "MoveUp":
                    _viewModel?.MoveEffectUpCommand.Execute(effect);
                    break;
                case "MoveDown":
                    _viewModel?.MoveEffectDownCommand.Execute(effect);
                    break;
                case "Duplicate":
                    // TODO: Implement effect duplication
                    break;
                case "Remove":
                    _viewModel?.RemoveEffectCommand.Execute(effect);
                    break;
            }
        }
    }

    // Section context menu handlers
    private void OnSectionContextMenu(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is AvsSection section)
        {
            var action = menuItem.Name;
            
            if (action == "ClearSection")
            {
                _viewModel?.ClearSectionCommand.Execute(section);
            }
        }
    }

    // Keyboard shortcuts
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            switch (e.Key)
            {
                case Key.N:
                    _viewModel?.NewPresetCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.O:
                    _viewModel?.LoadPresetCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.S:
                    _viewModel?.SavePresetCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.T:
                    _viewModel?.TestPresetCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
        else if (e.Key == Key.Delete && _viewModel?.SelectedEffect != null)
        {
            _viewModel.RemoveEffectCommand.Execute(_viewModel.SelectedEffect);
            e.Handled = true;
        }
    }

    // Helper method to refresh the UI
    public void RefreshUI()
    {
        // TODO: Implement UI refresh if needed
    }
}
