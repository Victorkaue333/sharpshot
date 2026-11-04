using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace SharpShot.App.Components;

/// <summary>Vector SharpShot mark (Concept A). Geometry comes from the generated <see cref="LogoGeometry"/>.</summary>
public sealed partial class LogoMark : UserControl
{
    public LogoMark()
    {
        InitializeComponent();
        TopBracketPath.Data = Parse(LogoGeometry.TopBracket);
        LowerSilverPath.Data = Parse(LogoGeometry.LowerSilver);
        LowerSilverEdgePath.Data = Parse(LogoGeometry.LowerSilverEdge);
        UpperSilverPath.Data = Parse(LogoGeometry.UpperSilver);
        UpperSilverEdgePath.Data = Parse(LogoGeometry.UpperSilverEdge);
        BottomBracketPath.Data = Parse(LogoGeometry.BottomBracket);
    }

    private static Geometry Parse(string pathData)
        => (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), pathData);
}
