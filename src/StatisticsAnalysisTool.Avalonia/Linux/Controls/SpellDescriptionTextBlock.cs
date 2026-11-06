// Avalonia port of Common/Controls/SpellDescriptionTextBlock.cs: renders the segments of a spell description as
// runs (bold, explicit RRGGBB color, or the Spell.Type.* brush of the segment type).
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using StatisticsAnalysisTool.Models.ItemDetailsModel;

namespace StatisticsAnalysisTool.Common.Controls;

public sealed class SpellDescriptionTextBlock : TextBlock
{
    public static readonly StyledProperty<IEnumerable<ItemSpellDescriptionSegment>> SegmentsProperty =
        AvaloniaProperty.Register<SpellDescriptionTextBlock, IEnumerable<ItemSpellDescriptionSegment>>(nameof(Segments));

    static SpellDescriptionTextBlock()
    {
        SegmentsProperty.Changed.AddClassHandler<SpellDescriptionTextBlock>((textBlock, _) => textBlock.UpdateInlines());
    }

    protected override Type StyleKeyOverride => typeof(TextBlock);

    public IEnumerable<ItemSpellDescriptionSegment> Segments
    {
        get => GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    protected override void OnAttachedToLogicalTree(global::Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        UpdateInlines(); // Spell.Type.* brushes can only be resolved once the control has a resource host.
    }

    private void UpdateInlines()
    {
        Inlines?.Clear();
        if (Segments == null)
        {
            return;
        }

        Inlines ??= new InlineCollection();
        foreach (var segment in Segments)
        {
            var run = new Run(segment.Text);
            if (TryGetForeground(segment, out var foreground))
            {
                run.Foreground = foreground;
            }

            if (segment.IsBold)
            {
                run.FontWeight = FontWeight.Bold;
            }

            Inlines.Add(run);
        }
    }

    private bool TryGetForeground(ItemSpellDescriptionSegment segment, out IBrush foreground)
    {
        if (TryCreateColorBrush(segment.ColorHex, out foreground))
        {
            return true;
        }

        var resourceKey = GetForegroundResourceKey(segment.TypeKey);
        foreground = !string.IsNullOrWhiteSpace(resourceKey)
                     && this.TryFindResource(resourceKey, ActualThemeVariant, out var resource)
            ? resource as IBrush
            : null;
        return foreground != null;
    }

    internal static bool TryCreateColorBrush(string colorHex, out IBrush brush)
    {
        brush = null;
        if (colorHex?.Length != 6
            || !byte.TryParse(colorHex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red)
            || !byte.TryParse(colorHex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green)
            || !byte.TryParse(colorHex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
        {
            return false;
        }

        brush = new ImmutableSolidColorBrush(Color.FromRgb(red, green, blue));
        return true;
    }

    internal static string GetForegroundResourceKey(string typeKey)
    {
        return typeKey switch
        {
            "damage" => "Spell.Type.Damage",
            "heal" => "Spell.Type.Heal",
            "crowdcontrol" => "Spell.Type.CrowdControl",
            "debuff" => "Spell.Type.Debuff",
            "buff" => "Spell.Type.Buff",
            "movement" => "Spell.Type.Mobility",
            "other" => "Spell.Type.Other",
            _ => string.Empty
        };
    }
}
