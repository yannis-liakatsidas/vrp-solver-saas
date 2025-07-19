namespace Helpers
{
    public sealed class ExecutionNumber
    {
        private static int executionNumber = 0;

        private static readonly Lazy<ExecutionNumber> lazyInstance =
            new Lazy<ExecutionNumber>(() => new ExecutionNumber(), isThreadSafe: true);

        public static ExecutionNumber GetOrCreateInstance => lazyInstance.Value;

        private ExecutionNumber()
        {
            executionNumber++;
        }

        public int GetNextExecutionNumber() => executionNumber;
    }
}
