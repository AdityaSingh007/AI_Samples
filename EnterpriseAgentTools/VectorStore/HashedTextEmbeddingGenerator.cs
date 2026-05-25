namespace CAEAgentTools.VectorStore
{
    internal sealed class HashedTextEmbeddingGenerator(int vectorSize = 256)
    {
        private static readonly char[] TokenSeparators = [' ', '\t', '\r', '\n', ',', '.', ';', ':', '-', '_', '/', '\\', '|', '(', ')', '[', ']', '{', '}', '"', '\'', '!', '?'];

        public float[] Generate(string? text)
        {
            var vector = new float[vectorSize];

            if (string.IsNullOrWhiteSpace(text))
            {
                return vector;
            }

            foreach (var token in text.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var index = (int)((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(token) % (uint)vectorSize);
                vector[index] += 1f;
            }

            Normalize(vector);
            return vector;
        }

        private static void Normalize(float[] vector)
        {
            double magnitude = 0;

            foreach (var value in vector)
            {
                magnitude += value * value;
            }

            if (magnitude == 0)
            {
                return;
            }

            var scale = 1d / Math.Sqrt(magnitude);

            for (var index = 0; index < vector.Length; index++)
            {
                vector[index] = (float)(vector[index] * scale);
            }
        }
    }
}
