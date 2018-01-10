// Edge.cs
// Copyright Karel Kroeze, 2018-2018
namespace FluffyResearchTree
{
    public class Edge<T1, T2> where T1 : Node where T2 : Node
    {
        private T1 _in;
        private T2 _out;
        private bool _dummy;
        public T1 In
        {
            get => _in;
            set
            {
                _in = value;
                _dummy = _out is DummyNode || _in is DummyNode;
            } 
        }
        public T2 Out
        {
            get => _out;
            set
            {
                _out = value;
                _dummy = _out is DummyNode || _in is DummyNode;
            }
        }

        public Edge( T1 @in, T2 @out )
        {
            _in = @in;
            _out = @out;
            _dummy = _out is DummyNode || _in is DummyNode;
        }

        public int Span => _out.X - _in.X;
        public bool IsDummy => _dummy;

        public override string ToString()
        {
            return _in + " -> " + _out;
        }
    }
}