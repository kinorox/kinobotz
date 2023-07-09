namespace Entities.Exceptions
{
    public class InvalidCommandOperationException : InvalidCommandException
    {
        public InvalidCommandOperationException()
        {

        }

        public InvalidCommandOperationException(string message) : base(message)
        {

        }
    }
}
