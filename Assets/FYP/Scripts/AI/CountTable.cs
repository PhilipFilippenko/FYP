namespace FYP
{
    // a table that counts what the player did next. old counts slowly fade.
    // used twice: rows = last action (markov chain) and rows = boss attack (answers).
    // no random here, so the same input always gives the same result
    public class CountTable
    {
        public int rows;
        public int columns;
        public float[,] counts;
        public float decay;
        public float smoothing;

        // empty table
        public CountTable(int rows, int columns, float decay, float smoothing)
        {
            this.rows = rows;
            this.columns = columns;
            counts = new float[rows, columns];
            this.decay = decay;
            this.smoothing = smoothing;
        }

        // count one new thing
        public void Add(int row, int column)
        {
            // old counts in this row get smaller, so the table follows a change in how the player plays
            for (int c = 0; c < columns; c++)
                counts[row, c] *= decay;
            counts[row, column] += 1;
        }

        // how much data a row has
        public float RowTotal(int row)
        {
            float total = 0;
            for (int c = 0; c < columns; c++)
                total += counts[row, c];
            return total;
        }

        // chance of each column. a small extra keeps actions we never saw above zero
        public float[] Predict(int row)
        {
            float[] probabilities = new float[columns];
            float total = RowTotal(row);
            for (int c = 0; c < columns; c++)
                probabilities[c] = (counts[row, c] + smoothing) / (total + smoothing * columns);
            return probabilities;
        }
    }
}
