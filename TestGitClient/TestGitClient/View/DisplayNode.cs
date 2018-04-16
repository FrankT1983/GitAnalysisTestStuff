using System.Windows;
using System.Windows.Media;

namespace TestGitClient
{
    internal class DisplayNode : DependencyObject
    {


        public Brush Color
        {
            get { return (Brush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Color.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Brush), typeof(DisplayNode), new PropertyMetadata(Brushes.White));
        
        public bool Highlight
        {
            get { return (bool)GetValue(HighlightProperty); }
            set { SetValue(HighlightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Highlight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighlightProperty =
            DependencyProperty.Register("Highlight", typeof(bool), typeof(DisplayNode), new PropertyMetadata(false));

        public Node Node
        {
            get { return (Node)GetValue(NodeProperty); }
            set { SetValue(NodeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Node.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NodeProperty =
            DependencyProperty.Register("Node", typeof(Node), typeof(DisplayNode), new PropertyMetadata(null));                      
       
        public DisplayNode(Node n )
        {
            this.Node = n;

            switch(n.Type)
            {
                case Node.NodeType.Commit:
                    this.Color = (SolidColorBrush)(new BrushConverter().ConvertFrom("#f00000")); break;
                case Node.NodeType.FileCS:
                    this.Color = (SolidColorBrush)(new BrushConverter().ConvertFrom("#c6f1ff")); break;
                case Node.NodeType.Syntax:
                    this.Color = (SolidColorBrush)(new BrushConverter().ConvertFrom("#c6ffe3")); break;

                default:
                    this.Color = Brushes.White;break;
            }
        }       

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(DisplayNode), new PropertyMetadata(false));



    }
}
