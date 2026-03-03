namespace Compute
{
    /// <summary>
    /// Ergonomic builder for kernel launch dimensions.
    /// </summary>
    public readonly struct Grid
    {
        private readonly WorkerDimensions _workers;

        private Grid(WorkerDimensions workers)
        {
            _workers = workers;
        }

        public static Grid Size(uint x) => new(new WorkerDimensions(x));

        public static Grid Size(uint x, uint y) => new(new WorkerDimensions(x, y));

        public static Grid Size(uint x, uint y, uint z) => new(new WorkerDimensions(x, y, z));

        public static Grid Size(int x) => Size((uint)x);

        public static Grid Size(int x, int y) => Size((uint)x, (uint)y);

        public static Grid Size(int x, int y, int z) => Size((uint)x, (uint)y, (uint)z);

        public Grid Tile(uint localX, uint localY = 0, uint localZ = 0)
        {
            var localYValue = localY == 0 && _workers.Y > 0 ? 1u : localY;
            var localZValue = localZ == 0 && _workers.Z > 0 ? 1u : localZ;

            return new Grid(new WorkerDimensions
            {
                X = _workers.X,
                Y = _workers.Y,
                Z = _workers.Z,
                LocalX = localX,
                LocalY = localYValue,
                LocalZ = localZValue
            });
        }

        public static implicit operator WorkerDimensions(Grid grid) => grid._workers;
    }
}