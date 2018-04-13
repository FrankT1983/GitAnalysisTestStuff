using System.Windows;

namespace TestGitClient
{
    internal class DisplayNode : DependencyObject
    {


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
