using Microsoft.Extensions.Logging;

namespace Paramore.Fences.TestUtils;
#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.

public record class LogRecord(LogLevel LogLevel, EventId EventId, string Message, Exception? Exception, object State);
