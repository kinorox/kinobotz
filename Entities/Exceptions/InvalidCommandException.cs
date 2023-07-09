namespace Entities.Exceptions
{
    public class InvalidCommandException : Exception
    {
        public InvalidCommandException() : base("Invalid command.")
        {

        }
        public InvalidCommandException(string message) : base($"Invalid command. {message}")
        {

        }
    }
}
