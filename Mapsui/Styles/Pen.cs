namespace Mapsui.Styles
{
    public class Pen
    {
        private PenStyle _penStyle = PenStyle.Solid;
        private PenStrokeCap _penStrokeCap = PenStrokeCap.Butt;

        public Pen() {}

        public Pen(Color color, double width = 1)
        {
            Color = color;
            Width = width;
        }

        public double Width { get; set; } = 1;
        public Color Color { get; set; }

        public PenStyle PenStyle
        {
            get { return _penStyle; }
            set { _penStyle = value; }
        }

        public PenStrokeCap PenStrokeCap
        {
            get { return _penStrokeCap; }
            set { _penStrokeCap = value; }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Pen))
            {
                return false;
            }
            return Equals((Pen)obj);
        }

        public bool Equals(Pen pen)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (Width != pen.Width)
            {
                return false;
            }

            if ((Color == null) ^ (pen.Color == null)) //if one or the other is null then they are not equal, but not when they are both null
            {
                return false;
            }

            if (Color != null && !Color.Equals(pen.Color))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return Width.GetHashCode() ^ (Color == null ? 0 : Color.GetHashCode());
        }

        public static bool operator ==(Pen pen1, Pen pen2)
        {
            return Equals(pen1, pen2);
        }

        public static bool operator !=(Pen pen1, Pen pen2)
        {
            return !Equals(pen1, pen2);
        }
    }
}
