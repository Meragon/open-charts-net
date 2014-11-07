using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OpenCharts
{
    public partial class cChart : UserControl
    {
        public cChart()
        {
            InitializeComponent();

            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);

            _pointTooltip.OwnerDraw = true;
            _pointTooltip.Draw += _pointTooltip_Draw;
            _pointTooltip.Popup += _pointTooltip_Popup;
        }

        private void _pointTooltip_Popup(object sender, PopupEventArgs e)
        {
            e.ToolTipSize = new Size((int)_pointTooltipInfo.Width, (int)_pointTooltipInfo.Height);
        }
        private void _pointTooltip_Draw(object sender, DrawToolTipEventArgs e)
        {
            Graphics gr = e.Graphics;
            gr.Clear(Color.White);

            float width = _pointTooltipInfo.Width;
            float height = _pointTooltipInfo.Height;

            // Shadow.
            // g.FillRectangle(new SolidBrush(Color.FromArgb(50, 0, 0, 0)), 2, height, width - 2, 2);
            //g.FillRectangle(new SolidBrush(Color.FromArgb(50, 0, 0, 0)), width, 2, 2, height);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
            //g.FillRectangle(new SolidBrush(Color.FromArgb(150, 255, 255, 255)), 0, 0, width - 1, height - 1);

            int r = _pointTooltipInfo.Color.R - 50; r = r < 0 ? 0 : r;
            int g = _pointTooltipInfo.Color.G - 50; g = g < 0 ? 0 : g;
            int b = _pointTooltipInfo.Color.B - 50; b = b < 0 ? 0 : b;

            _pointTooltipInfo.Color = Color.FromArgb(r, g, b);
            gr.DrawRectangle(new Pen(_pointTooltipInfo.Color), 0, 0, width - 1, height - 1);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            gr.FillEllipse(new SolidBrush(_pointTooltipInfo.Color), 4, 19, 8, 8);
            gr.DrawString(xAxis.Categories[_pointTooltipInfo.CatIndex], xAxis.CategoriesFont, new SolidBrush(xAxis.CategoriesColor), new PointF(6, 0));
            gr.DrawString(_pointTooltipInfo.Value, xAxis.CategoriesFont, new SolidBrush(xAxis.CategoriesColor), new PointF(16, 16));
        }

        private cxAxis _xAxis = new cxAxis();
        [Category("Charts"), TypeConverter(typeof(cxAxis_Converter))]
        public cxAxis xAxis
        {
            get { return _xAxis; }
            set { _xAxis = value; }
        }

        private cyAxis _yAxis = new cyAxis();
        [Category("Charts"), TypeConverter(typeof(cyAxis_Converter))]
        public cyAxis yAxis
        {
            get { return _yAxis; }
            set { _yAxis = value; }
        }

        private cSeries[] _Series = null;
        [Category("Charts"), TypeConverter(typeof(cSeries_Converter))]
        public cSeries[] Series
        {
            get { return _Series; }
            set { _Series = value; }
        }

        private Color[] _seriesColorCollection = new Color[]
        {
            Color.FromArgb(0x7C, 0xB5, 0xEC),
            Color.FromArgb(0x43, 0x43, 0x48),
            Color.FromArgb(0x90, 0xED, 0x7D),
            Color.FromArgb(0xF7, 0xA3, 0x5C),
            Color.FromArgb(0x80, 0x85, 0xE9),
            Color.FromArgb(0xF1, 0x5C, 0x80),
            Color.FromArgb(0xE4, 0xD3, 0x54),
            Color.FromArgb(0x80, 0x85, 0xE8),
            Color.FromArgb(0x8D, 0x46, 0x53),
            Color.FromArgb(0x91, 0xE8, 0xE1)
        };
        [Category("Charts")]
        public Color[] SeriesColorCollection
        {
            get { return _seriesColorCollection; }
            set { _seriesColorCollection = value; }
        }

        private Font _seriesFont = new Font("Segoe UI", 10);
        [Category("Charts")]
        public Font SeriesFont
        {
            get { return _seriesFont; }
            set { _seriesFont = value; }
        }

        private Color _seriesGridColor = Color.FromArgb(70, 70, 70);
        [Category("Charts")]
        public Color SeriesGridColor
        {
            get { return _seriesGridColor; }
            set { _seriesGridColor = value; }
        }

        private Color _bottomLineColor = Color.FromArgb(180, 190, 230);
        [Category("Charts")]
        public Color BottomLineColor
        {
            get { return _bottomLineColor; }
            set { _bottomLineColor = value; }
        }

        // Paint tools.
        private float _serScale = 1;
        private float _serCatOffset = 0;
        private eScrolling _serScrolling = eScrolling.None;
        private float _serScrollingPrevMouseX = 0;

        private string _serFormat = "0";
        private PointF _serUpperPoint;
        private PointF _serLowerPoint;
        private Point _serSelectedPoint = new Point(-1, -1);

        private Point _pointBottomLeft;
        private int _pointBottomLeft_LeftOffset = 64;
        private int _pointBottomLeft_BottomOffset = 16;
        private Point _pointBottomRight;
        private int _pointBottomRight_RightOffset = 16;
        private int _pointBottomRight_BottomOffset = 16;

        private int _catsVisible = 0;
        private float _catSkipFactor = 1;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            _RecalculateDraw();

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int _catsWidth = _pointBottomRight.X - _pointBottomLeft.X; // Yep, cats.
            float _widthPerCat = (float)_catsWidth / (float)xAxis.Categories.Length;
            _catsVisible = xAxis.Categories.Length - (int)_serCatOffset;
            _catSkipFactor = 1;
            if (_widthPerCat < xAxis.CategoriesMinWidth)
            {
                _catsVisible = _catsWidth / xAxis.CategoriesMinWidth;
                _widthPerCat = _catsWidth / _catsVisible;
                _catSkipFactor = ((float)xAxis.Categories.Length) / (float)_catsVisible;
                _catSkipFactor /= _serScale;
                if (_catsVisible >= (xAxis.Categories.Length / (_serScale)))
                    _catSkipFactor = 1; // todo: not actually fix. 
                if (_catSkipFactor == 0)
                {
                    _catSkipFactor = 1;
                    //_serScale = _serScale > 1.5f ? _serScale - 1 : 1;
                }
            }

            int _catsToCount = xAxis.Categories.Length / (int)_serScale;

            // yAxis.
            if (!String.IsNullOrEmpty(yAxis.Title.Text))
            {
                g.RotateTransform(270);
                StringFormat _titleFormat = new StringFormat();
                _titleFormat.Alignment = (StringAlignment)(int)yAxis.Title.Aligment;
                g.DrawString(yAxis.Title.Text, yAxis.Title.Font, new SolidBrush(xAxis.CategoriesColor), new RectangleF(-(_serLowerPoint.Y), 0, _serLowerPoint.Y - _serUpperPoint.Y, 20), _titleFormat);
                g.RotateTransform(90);
            }

            string _tooltipValue = String.Empty;
            Color _tooltipColor = Color.White;

            // Series.
            if (_Series != null && _Series.Length > 0)
            {
                // Set upper && lowe limits.
                float _upperLimit = 0;
                float _lowerLimit = 0;
                foreach (cSeries _ser in Series)
                {

                    if (_ser.Data != null)
                        foreach (string _data in _ser.Data)
                        {
                            double _ddata = 0;
                            if (double.TryParse(_data, out _ddata))
                            {
                                if (_ddata > _upperLimit)
                                    _upperLimit = (float)_ddata;
                                if (_ddata < _lowerLimit)
                                    _lowerLimit = (float)_ddata;
                            }
                        }
                }
                _upperLimit += _upperLimit * .1f;
                if (_lowerLimit < 0)
                    _lowerLimit += _lowerLimit * .1f;
                else
                    _lowerLimit += _lowerLimit / .1f;

                float _pixelsPerPoint = (_serLowerPoint.Y - _serUpperPoint.Y) / (_upperLimit - _lowerLimit);

                // Draw grid.
                int _gridLinesCount = 8;
                float _gridLinesHeight = (_serLowerPoint.Y - _serUpperPoint.Y) / _gridLinesCount;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
                for (int i = 0; i < _gridLinesCount; i++)
                {
                    PointF _gridPoint = new PointF(_pointBottomLeft_LeftOffset, _serLowerPoint.Y - _gridLinesHeight * (i));
                    if (i != 0) g.DrawLine(Pens.LightGray, _gridPoint.X, _gridPoint.Y, this.Width - _pointBottomRight_RightOffset, _gridPoint.Y);
                    float _gridPointValue = (((i) * _gridLinesHeight) / _pixelsPerPoint) + _lowerLimit;
                    g.DrawString(_TransformNumberToShort(_gridPointValue), _seriesFont, new SolidBrush(_seriesGridColor), new PointF(_gridPoint.X - 36, _gridPoint.Y - 10));
                }

                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                int _serPointColor_current = 0;
                Color _serPointColor = _seriesColorCollection[_serPointColor_current];

                if (xAxis.Categories != null)
                {
                    int columnscount = Series.Count(x => x.Type == cSeries.eType.Column);
                    float columnwidth = ((_widthPerCat - 8) / columnscount) / _catSkipFactor;
                    int columnindex = 0;

                    for (int k = 0; k < Series.Length; k++)
                    {
                        if (Series[k].Data != null)
                        {
                            List<cSeries.Point> _serPoints = new List<cSeries.Point>();
                            for (int i = (int)_serCatOffset; i < Series[k].Data.Length && i < _catsToCount + _serCatOffset; i++)
                            {
                                double _datavalue = 0;
                                if (double.TryParse(Series[k].Data[i], out _datavalue))
                                    _serPoints.Add(new cSeries.Point(
                                        _pointBottomLeft.X + ((float)_catsWidth / (float)_catsToCount) / 2 + ((float)_catsWidth / (float)_catsToCount) * (i - (int)_serCatOffset),
                                        _serLowerPoint.Y - ((float)_datavalue - _lowerLimit) * _pixelsPerPoint));
                                else
                                    _serPoints.Add(null);
                            }

                            SolidBrush _serEllipseBrush = new SolidBrush(_serPointColor);
                            int ar = _serPointColor.R; ar = ar < 200 ? ar + 50 : 250;
                            int ag = _serPointColor.G; ag = ag < 200 ? ag + 50 : 250;
                            int ab = _serPointColor.B; ab = ab < 200 ? ab + 50 : 250;
                            int aa = 100;
                            SolidBrush _areaBrush = new SolidBrush(Color.FromArgb(aa, ar, ag, ab));

                            Pen _serLinePen = new Pen(_serPointColor);
                            _serLinePen.Width = 2 / _catSkipFactor;
                            if (_serLinePen.Width == 0)
                                _serLinePen.Width = 1;

                            for (int i = 0; i < _serPoints.Count; i++)
                            {
                                if (_serPoints[i] != null)
                                {
                                    switch (Series[k].Type)
                                    {
                                        case cSeries.eType.Line:
                                            g.FillEllipse(_serEllipseBrush, _serPoints[i].X, _serPoints[i].Y, 8 / _catSkipFactor, 8 / _catSkipFactor);
                                            if (i + 1 < _serPoints.Count && _serPoints[i + 1] != null)
                                                g.DrawLine(_serLinePen, _serPoints[i].X + 4 / _catSkipFactor, _serPoints[i].Y + 4 / _catSkipFactor, _serPoints[i + 1].X + 4 / _catSkipFactor, _serPoints[i + 1].Y + 4 / _catSkipFactor);
                                            break;
                                        case cSeries.eType.Column:
                                            g.FillRectangle(_serEllipseBrush, _serPoints[i].X + (columnscount > 1 ? - columnwidth * columnscount / 2 + columnwidth * columnindex : 0), _serPoints[i].Y, columnwidth, _serLowerPoint.Y - _serPoints[i].Y);
                                            break;
                                        case cSeries.eType.Area:
                                            if (i + 1 < _serPoints.Count && _serPoints[i + 1] != null)
                                            {
                                                List<PointF> _areaPoints = new List<PointF>();
                                                _areaPoints.Add(new PointF(_serPoints[i].X, _serPoints[i].Y));
                                                _areaPoints.Add(new PointF(_serPoints[i + 1].X, _serPoints[i + 1].Y));
                                                _areaPoints.Add(new PointF(_serPoints[i + 1].X, _serLowerPoint.Y));
                                                _areaPoints.Add(new PointF(_serPoints[i].X, _serLowerPoint.Y));
                                                g.FillPolygon(_areaBrush, _areaPoints.ToArray());
                                                g.DrawLine(_serLinePen, _serPoints[i].X, _serPoints[i].Y, _serPoints[i + 1].X, _serPoints[i + 1].Y);
                                            }
                                            else
                                                g.FillEllipse(_serEllipseBrush, _serPoints[i].X - 4, _serPoints[i].Y - 4, 8 / _catSkipFactor, 8 / _catSkipFactor);
                                            break;
                                    }
                                }
                            }
                            if (Series[k].Type == cSeries.eType.Column)
                                columnindex++;
                        }
                        // Next color.
                        _serPointColor_current++;
                        if (_seriesColorCollection.Length <= _serPointColor_current)
                            _serPointColor_current = 0;
                        _serPointColor = _seriesColorCollection[_serPointColor_current];
                    }
                }
            }

            // Bottom line.
            int _bottomLineHeight = 4;
            Pen _bottomLinePen = new Pen(_bottomLineColor);
            g.DrawLine(_bottomLinePen, _pointBottomLeft, _pointBottomRight);
            g.DrawLine(_bottomLinePen, _pointBottomLeft, new Point(_pointBottomLeft.X, _pointBottomLeft.Y + _bottomLineHeight));
            g.DrawLine(_bottomLinePen, _pointBottomRight, new Point(_pointBottomRight.X, _pointBottomRight.Y + _bottomLineHeight));

            // Categories.
            if (xAxis.Categories != null)
            {
                SolidBrush _catBrush = new SolidBrush(xAxis.CategoriesColor);
                StringFormat _catFormat = new StringFormat();
                _catFormat.Alignment = StringAlignment.Center;

                int _catIndex = 0;
                for (float i = 0; _catIndex < _catsVisible && i + _serCatOffset < xAxis.Categories.Length; i += _catSkipFactor, _catIndex++)
                {
                    Point _linePoint = new Point(_pointBottomLeft.X + (_catIndex + 1) * (int)_widthPerCat, _pointBottomLeft.Y);
                    if (_catIndex + 1 != _catsVisible)
                        g.DrawLine(_bottomLinePen, _linePoint, new Point(_linePoint.X, _linePoint.Y + _bottomLineHeight));
                    if (i + _serCatOffset < xAxis.Categories.Length)
                    {
                        float temp = 0;
                        try
                        {
                            string catvalue = xAxis.Categories[(int)_serCatOffset + (int)Math.Ceiling(i)];
                            if (float.TryParse(catvalue, out temp))
                                catvalue = _TransformNumberToShort(temp);
                            g.DrawString(catvalue, xAxis.CategoriesFont, _catBrush, new RectangleF(_linePoint.X - _widthPerCat, _linePoint.Y + 2, _widthPerCat, 14), _catFormat);
                        }
                        catch (IndexOutOfRangeException)
                        {
                            // Dunno why.
                            _serCatOffset -= 2;
                            i -= _catSkipFactor;
                            _catIndex--;
                        }
                    }
                }
            }

            if (_serScale > 1)
                g.DrawString((_serScale * 100).ToString() + "%", _seriesFont, new SolidBrush(_seriesGridColor), this.Width - 48, 0);

            // Scrollbar.
            if (_serScale > 1)
            {
                int maxoffset = xAxis.Categories.Length - _catsVisible;
                if (maxoffset > 0)
                {
                    float scrollbarwidth = _pointBottomRight.X - _pointBottomLeft.X;
                    float scrollwidth = ((float)_pointBottomRight.X - (float)_pointBottomLeft.X - 4) / _serScale;
                    float tsw = scrollbarwidth - scrollwidth - 4;

                    g.DrawRectangle(Pens.LightGray, _pointBottomLeft.X, _pointBottomLeft.Y - 8, scrollbarwidth, 8);


                    g.FillRectangle(new SolidBrush(Color.Gray), _pointBottomLeft.X + 2 + tsw * ((float)_serCatOffset / ((float)xAxis.Categories.Length - _catsVisible * _catSkipFactor)), _pointBottomLeft.Y - 6, scrollwidth, 4);
                }
            }
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            // TODO: rework here.
            return;

            int _catsWidth = _pointBottomRight.X - _pointBottomLeft.X; // Yep, cats.
            float _widthPerCat = (float)_catsWidth / (float)xAxis.Categories.Length;
            _catsVisible = xAxis.Categories.Length - (int)_serCatOffset;
            _catSkipFactor = 1;
            if (_widthPerCat < xAxis.CategoriesMinWidth)
            {
                _catsVisible = _catsWidth / xAxis.CategoriesMinWidth;
                _widthPerCat = _catsWidth / _catsVisible;
                _catSkipFactor = ((float)xAxis.Categories.Length) / (float)_catsVisible;
                _catSkipFactor /= _serScale;
                if (_catsVisible >= (xAxis.Categories.Length / (_serScale)))
                    _catSkipFactor = 1; // todo: not actually fix. 
                if (_catSkipFactor == 0)
                {
                    _catSkipFactor = 1;
                    //_serScale = _serScale > 1.5f ? _serScale - 1 : 1;
                }
            }

            int _catsToCount = xAxis.Categories.Length / (int)_serScale;

            // Series.
            if (_Series != null && _Series.Length > 0)
            {
                // Set upper && lowe limits.
                float _upperLimit = 0;
                float _lowerLimit = 0;
                foreach (cSeries _ser in Series)
                {

                    if (_ser.Data != null)
                        foreach (string _data in _ser.Data)
                        {
                            double _ddata = 0;
                            if (double.TryParse(_data, out _ddata))
                            {
                                if (_ddata > _upperLimit)
                                    _upperLimit = (float)_ddata;
                                if (_ddata < _lowerLimit)
                                    _lowerLimit = (float)_ddata;
                            }
                        }
                }
                _upperLimit += _upperLimit * .1f;
                if (_lowerLimit < 0)
                    _lowerLimit += _lowerLimit * .1f;
                else
                    _lowerLimit += _lowerLimit / .1f;

                float _pixelsPerPoint = (_serLowerPoint.Y - _serUpperPoint.Y) / (_upperLimit - _lowerLimit);

                int _serPointColor_current = 0;
                Color _serPointColor = _seriesColorCollection[_serPointColor_current];

                bool _clearSelected = false;
                int _sern = 0;
                if (xAxis.Categories != null)
                    for (int k = 0; k < Series.Length; k++)
                    {
                        if (Series[k].Data != null)
                        {
                            List<cSeries.Point> _serPoints = new List<cSeries.Point>();
                            for (int i = (int)_serCatOffset; i < Series[k].Data.Length && i < _catsToCount + _serCatOffset; i++)
                            {
                                double _datavalue = 0;
                                if (double.TryParse(Series[k].Data[i], out _datavalue))
                                    _serPoints.Add(new cSeries.Point(
                                        _pointBottomLeft.X + ((float)_catsWidth / (float)_catsToCount) / 2 + ((float)_catsWidth / (float)_catsToCount) * (i - (int)_serCatOffset),
                                        _serLowerPoint.Y - ((float)_datavalue - _lowerLimit) * _pixelsPerPoint));
                                else
                                    _serPoints.Add(null);
                            }


                            for (int i = 0; i < _serPoints.Count; i++)
                            {
                                if (_serPoints[i] != null)
                                {

                                    if (e.X > _serPoints[i].X - 8 && e.X < _serPoints[i].X + 16 &&
                                        e.Y > _serPoints[i].Y - 8 && e.Y < _serPoints[i].Y + 16)
                                    {
                                        if (_serSelectedPoint.X == _sern && _serSelectedPoint.Y == i)
                                            _clearSelected = true;
                                        else
                                        {
                                            _serSelectedPoint = new Point(_sern, i + (int)_serCatOffset);
                                            _pointTooltipInfo.Point = new PointF(_serPoints[i].X, _serPoints[i].Y);
                                            _pointTooltipInfo.CatIndex = i + (int)_serCatOffset;
                                            _pointTooltipInfo.Value = Series[k].Name + ": " + Series[k].Data[i + (int)_serCatOffset];
                                            _pointTooltipInfo.Color = _serPointColor;
                                            _pointTooltipInfo.Point = new PointF(_pointTooltipInfo.Point.X - _pointTooltipInfo.Width / 2, _pointTooltipInfo.Point.Y - _pointTooltipInfo.Height - 8);
                                            if (_pointTooltipInfo.Point.Y < 0)
                                                _pointTooltipInfo.Point = new PointF(_pointTooltipInfo.Point.X, _pointTooltipInfo.Point.Y + _pointTooltipInfo.Height + 12);
                                            // Shadow.
                                            _pointTooltip.Show("some", this, (int)_pointTooltipInfo.Point.X, (int)_pointTooltipInfo.Point.Y);
                                            //this.Refresh();
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        _sern++;
                        // Next color.
                        _serPointColor_current++;
                        if (_seriesColorCollection.Length <= _serPointColor_current)
                            _serPointColor_current = 0;
                        _serPointColor = _seriesColorCollection[_serPointColor_current];
                    }
                //if (_clearSelected)
                {
                    _serSelectedPoint = new Point(-1, -1);
                    _pointTooltip.Hide(this);
                }

            }
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta > 0)
            {
                if (_catsVisible < (xAxis.Categories.Length / (_serScale)))
                    _serScale++;
            }
            else
            {
                _serScale--;
            }
            if (_serScale <= 1)
            {
                _serScale = 1;
                _serCatOffset = 0;
                this.Refresh();
            }
            else
                this.Refresh();
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            // Check scrollbar.
            if (_serScale != 1)
                if (e.Location.Y > _pointBottomLeft.Y - 12 && e.Location.Y < _pointBottomLeft.Y + 4)
                {
                    _serScrollingPrevMouseX = e.Location.X;
                    _serScrolling = eScrolling.Horizontal;
                }
                else
                {
                    _serScrollingPrevMouseX = e.Location.X;
                    _serScrolling = eScrolling.HorizonatlReversed;
                }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_serScrolling != eScrolling.None)
                _serScrolling = eScrolling.None;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_serScrolling != eScrolling.None)
            {
                float scrollbarwidth = _pointBottomRight.X - _pointBottomLeft.X;
                float scrollwidth = ((float)_pointBottomRight.X - (float)_pointBottomLeft.X - 4) / _serScale;
                float tsw = scrollbarwidth - scrollwidth - 4;
                float pixelsPerCat = tsw * ((float)1 / ((float)xAxis.Categories.Length - _catsVisible * _catSkipFactor));
                float mcats = Math.Abs(e.Location.X - _serScrollingPrevMouseX);
                mcats /= pixelsPerCat;

                if (e.Location.X > _serScrollingPrevMouseX)
                {
                    _serCatOffset += _serScrolling == eScrolling.Horizontal ? mcats : -mcats;
                }
                else
                    _serCatOffset -= _serScrolling == eScrolling.Horizontal ? mcats : -mcats; ;
                if (_serCatOffset < 0)
                    _serCatOffset = 0;
                else
                {

                    if (_serCatOffset > xAxis.Categories.Length - _catSkipFactor * _catsVisible)
                        _serCatOffset = Convert.ToInt32(xAxis.Categories.Length - _catSkipFactor * _catsVisible);
                    else
                        this.Refresh();
                }
                _serScrollingPrevMouseX = e.Location.X;
            }
        }

        private void _RecalculateDraw()
        {
            _pointBottomLeft = new Point(_pointBottomLeft_LeftOffset, this.Height - _pointBottomLeft_BottomOffset);
            _pointBottomRight = new Point(this.Width - _pointBottomRight_RightOffset, this.Height - _pointBottomRight_BottomOffset);

            _serUpperPoint = new PointF(_pointBottomLeft_LeftOffset - 16, 0);
            _serLowerPoint = new PointF(_pointBottomLeft_LeftOffset - 16, _pointBottomLeft.Y - _seriesFont.Height);
        }
        private string _TransformNumberToShort(float number)
        {
            string val = number.ToString(_serFormat);
            if (Math.Abs(number) > 1000000)
                val = val.Remove(val.Length - 6, 6) + "M";
            else
                if (Math.Abs(number) > 1000)
                    val = val.Remove(val.Length - 3, 3) + "K";

            return val;
        }

        private ToolTip _pointTooltip = new ToolTip();
        private TooltipInfo _pointTooltipInfo = new TooltipInfo();
    }
    public enum eScrolling
    {
        None, 
        Horizontal,
        HorizonatlReversed
    }

    public class cxAxis
    {
        private string[] _Categories;
        public string[] Categories
        {
            get { return _Categories; }
            set { _Categories = value; }
        }
        private Font _CategoriesFont = new Font("Segoe UI", 8, FontStyle.Bold);
        public Font CategoriesFont
        {
            get { return _CategoriesFont; }
            set { _CategoriesFont = value; }
        }
        private Color _CategoriesColor = Color.FromArgb(70, 70, 70);
        public Color CategoriesColor
        {
            get { return _CategoriesColor; }
            set { _CategoriesColor = value; }
        }
        private int _CategoriesMinWidth = 32;
        public int CategoriesMinWidth
        {
            get { return _CategoriesMinWidth; }
            set { _CategoriesMinWidth = value; }
        }
    }
    public class cxAxis_Converter : ExpandableObjectConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            cxAxis axis = (cxAxis)value;
            string cats = String.Empty;
            foreach (string cat in axis.Categories)
                cats += cat + "; ";
            return cats;
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
    
    public class cyAxis
    {
        private cTitle _Title = new cTitle();
        [TypeConverter(typeof(cTitle_Converter))]
        public cTitle Title
        {
            get { return _Title; }
            set { _Title = value; }
        }

        public class cTitle
        {
            private string _Text = String.Empty;
            public string Text
            {
                get { return _Text; }
                set { _Text = value; }
            }
            private Font _Font = new Font("Consolas", 12, FontStyle.Bold);
            public Font Font
            {
                get { return _Font; }
                set { _Font = value; }
            }
            private AligmentVertical _Aligment = AligmentVertical.Middle;

            public AligmentVertical Aligment
            {
                get { return _Aligment; }
                set { _Aligment = value; }
            }
        }
        public class cTitle_Converter : ExpandableObjectConverter
        {
            public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
            {
                return ((cTitle)value).Text;
            }
        }
    }
    public class cyAxis_Converter : ExpandableObjectConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            return String.Empty;
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public class cSeries
    {
        private string _Name = String.Empty;
        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }
        private string[] _Data;
        public string[] Data
        {
            get { return _Data; }
            set { _Data = value; }
        }
        private eType _Type = eType.Line;
        public eType Type
        {
            get { return _Type; }
            set { _Type = value; }
        }

        public class Point
        {
            public float X;
            public float Y;
            public Point(float x, float y)
            {
                X = x;
                Y = y;
            }
            public bool Selected;
        }
        public enum eType
        {
            Line,
            Column,
            Area
        }
    }
    public class cSeries_Converter : TypeConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            cSeries[] val = (cSeries[])value;
            string sval = String.Empty;
            foreach (cSeries s in val)
                sval += s.Name + "; ";
            return sval;
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public enum AligmentHorizontal
    {
        Left,
        Center,
        Right
    }
    public enum AligmentVertical
    {
        Low,
        Middle,
        High
    }

    public class TooltipInfo
    {
        public PointF Point = new PointF();
        public int CatIndex = -1;
        public string Value = String.Empty;
        public Color Color = Color.White;
        public float Width = 104;
        public float Height = 32;
    }
}
