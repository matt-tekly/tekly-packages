using System.Runtime.CompilerServices;

// DevBoard's own front ends drive the rebuild queue and other internals of the core
[assembly: InternalsVisibleTo("Tekly.DevBoard.UI")]
[assembly: InternalsVisibleTo("Tekly.DevBoard.Remote")]
[assembly: InternalsVisibleTo("Tekly.DevBoard.Editor")]
