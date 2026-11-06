// Compatibility shim: DependencyObject / DataTemplate / DataTemplateSelector for linked ViewModel-side
// selectors. A selector is also an Avalonia IDataTemplate so ported views can use it as ItemTemplate.
using Avalonia.Controls.Templates;

namespace System.Windows
{
    /// <summary>Thin wrapper standing in for WPF DependencyObject (the container passed to selectors).</summary>
    public class DependencyObject(Avalonia.AvaloniaObject source = null)
    {
        public Avalonia.AvaloniaObject Source { get; } = source;
    }

    public class DataTemplate(IDataTemplate template = null)
    {
        public IDataTemplate Template { get; } = template;
    }
}

namespace System.Windows.Controls
{
    public abstract class DataTemplateSelector : IDataTemplate
    {
        public virtual DataTemplate SelectTemplate(object item, DependencyObject container) => null;

        public bool Match(object data) => true;

        public Avalonia.Controls.Control Build(object param)
        {
            var template = SelectTemplate(param, new DependencyObject());
            return template?.Template?.Build(param);
        }
    }
}
