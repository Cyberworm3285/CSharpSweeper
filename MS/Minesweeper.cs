using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MS
{
    public partial class Minesweeper : Form
    {
        private const int FIELDS_WIDTH = 60;
        private const int FIELDS_HEIGHT = 40;
        private const int FIELDS_SIZE = FIELDS_WIDTH * FIELDS_HEIGHT;
        private const int FIELD_PIXEL = 25;
        private const int MINE_COUNT = 300;
        private const string TITLE = "Minesweeper";

        private Field[][] _fields;
        private int _counter;
        private int _target;
        private int _flagged;

        public Minesweeper()
        {
            InitializeComponent();
            InitializeFields(new Point(0, 0));
            Text = TITLE;
        }

        IEnumerable<(int x, int Y)> GetAllFieldPositions()
        {
            for (int x = 0; x < FIELDS_WIDTH; x++)
                for (int y = 0; y < FIELDS_HEIGHT; y++)
                    yield return (x, y);
        }

        protected HashSet<(int X, int Y)> GetMinePositions(int count, Random rand)
        => GetAllFieldPositions()
            .OrderBy(e => rand.Next())
            .Take(Math.Min(count, FIELDS_HEIGHT * FIELDS_WIDTH))
            .ToHashSet();

        protected IEnumerable<(int X, int Y)> GetSurrounding(int x, int y)
        {
            int lowX = x - 1;
            int lowY = y - 1;
            int highX = x + 1;
            int highY = y + 1;

            if (lowX < 0)
                lowX = 0;
            if (lowY < 0)
                lowY = 0;
            if (highX == FIELDS_WIDTH)
                highX = FIELDS_WIDTH - 1;
            if (highY == FIELDS_HEIGHT)
                highY = FIELDS_HEIGHT - 1;

            for (int i = lowX; i <= highX ; i++)
            {
                for (int j = lowY; j <= highY; j++)
                {
                    yield return (i, j);
                }
            }
        }

        protected void InitializeFields(Point anchorPoint)
        {
            var minePositions = GetMinePositions(MINE_COUNT, new Random());

            _target = FIELDS_SIZE - MINE_COUNT;
            _counter = 0;
            _flagged = 0;

            _fields = new Field[FIELDS_WIDTH][];
            for (int i = 0; i < FIELDS_WIDTH; i++)
            {
                _fields[i] = new Field[FIELDS_HEIGHT];
                for (int j = 0; j < FIELDS_HEIGHT; j++)
                {
                    bool isMine = minePositions.Contains((i, j));
                    int minesInProximity = GetSurrounding(i, j)
                        .Count(minePositions.Contains);

                    var field = new Field(i, j, isMine, minesInProximity)
                    {
                        Left = i * FIELD_PIXEL + anchorPoint.X,
                        Top = j * FIELD_PIXEL + anchorPoint.Y,
                        Name = $"{i}:{j}",
                        FlatStyle = FlatStyle.Flat,
                        Height = FIELD_PIXEL,
                        Width = FIELD_PIXEL,
                    };

                    field.MouseDown += Field_Click;

                    _fields[i][j] = field;
                    this.Controls.Add(field);
                }
            }
        }

        private void FlagField(Field field)
        {
            _flagged += field.ToggleFlag();
            Text = $"{TITLE} {_flagged}/{MINE_COUNT}";
        }

        private void Field_Click(object sender, MouseEventArgs e)
        {
            if (sender is Field field && !field.Checked)
            {
                if (e.Button == MouseButtons.Left && !field.Flagged)
                    CheckMine(field);
                else if (e.Button == MouseButtons.Right)
                    FlagField(field);
            }
        }

        protected void CheckMine(Field field)
        {
            if (field.IsMine)
            {
                var dontCare = DialogResult.No == MessageBox.Show(this, "Dat war scheise", "Junge", MessageBoxButtons.YesNo, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);

                if (dontCare)
                {
                    FlagField(field);
                    return;
                }

                Restart();
                return;
            }

            field.Check();
            _counter++;

            if (_counter == _target)
            {
                MessageBox.Show(this, "Feddich", "Geil Junge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Restart();
                return;
            }

            if (field.MinesInProximity != 0)
                return;

            var surroundings = GetSurrounding(field.X, field.Y)
                .Select(c => _fields[c.X][c.Y])
                .Where(f => !f.Checked);

            foreach (var s in surroundings)
                CheckMine(s);
        }

        private void Restart()
        {
            _counter = 0;
            _flagged = 0;
            var minePositions = GetMinePositions(MINE_COUNT, new Random());

            for (int i = 0; i < FIELDS_WIDTH; i++)
            {
                for (int j = 0; j < FIELDS_HEIGHT; j++)
                {
                    var field = _fields[i][j];

                    bool isMine = minePositions.Contains((i, j));
                    int minesInProximity = GetSurrounding(i, j)
                        .Count(minePositions.Contains);

                    field.Initialize(isMine, minesInProximity);
                }
            }
            Text = TITLE;
        }
    }

    public class FieldAppearence
    {
        private readonly Color _backgroundColor;
        private readonly Color _foreColor;
        private readonly Color _borderColor;
        private readonly string _text;

        public Color BackColor => _backgroundColor;
        public Color ForeColor => _foreColor;
        public Color BorderColor => _borderColor;
        public string Text => _text;

        public FieldAppearence(Color backColor, Color foreColor, Color borderColor, string text)
        {
            _backgroundColor = backColor;
            _foreColor = foreColor;
            _borderColor = borderColor;

            _text = text;
        }
    }

    public class Field : Button
    {
        private int _x;
        private int _y;
        private bool _isMine;
        private int _minesInProximity;
        private bool _checked;
        private bool _flagged;

        private readonly static Color[] _colors;

        public int X => _x;
        public int Y => _y;
        public bool IsMine => _isMine;
        public int MinesInProximity => _minesInProximity;
        public bool Checked => _checked;
        public bool Flagged => _flagged;

        public virtual FieldAppearence DefaultAppearance => new FieldAppearence
        (
            Color.DarkGray,
            Color.Black,
            Color.DarkGray,
            string.Empty
        );

        public virtual FieldAppearence FlaggedAppearance => new FieldAppearence
        (
            Color.Orange,
            Color.DarkRed,
            Color.DarkOrange,
            "!"
        );

        public virtual FieldAppearence CheckedApperance => new FieldAppearence
        (
            Color.LightGray,
            _colors[_minesInProximity],
            Color.LightGray,
            _minesInProximity == 0 
                ? string.Empty
                : _minesInProximity.ToString()
        );

        static Field()
        {
            _colors = new[]
            {
                DefaultBackColor,
                Color.Blue,
                Color.DarkGreen,
                Color.Brown,
                Color.DarkBlue,
                Color.Red,
                Color.Violet,
                Color.DarkRed,
                Color.DarkOrange,
            };
        }

        public Field(int x, int y, bool isMine, int minesInProximity)
        {
            _x = x;
            _y = y;

            Initialize(isMine, minesInProximity);
        }

        protected void SetAppearance(FieldAppearence appearence)
        {
            BackColor = appearence.BackColor;
            ForeColor = appearence.ForeColor;
            FlatAppearance.BorderColor = appearence.BorderColor;
            Text = appearence.Text;
        }

        public void Initialize(bool isMine, int minesInProximity)
        {
            _isMine = isMine;
            _minesInProximity = minesInProximity;

            _checked = false;
            _flagged = false;

            SetAppearance(DefaultAppearance);
        }

        public void Check()
        {
            _checked = true;
            SetAppearance(CheckedApperance);
        }

        public void Flag()
        {
            _flagged = true;
            SetAppearance(FlaggedAppearance);
        }

        public void UnFlag()
        {
            _flagged = false;
            SetAppearance(DefaultAppearance);
        }

        public int ToggleFlag()
        {
            if (_flagged)
            {
                UnFlag();
                return -1;
            }
            else
            {
                Flag();
                return 1;
            }
        }
    }
}
