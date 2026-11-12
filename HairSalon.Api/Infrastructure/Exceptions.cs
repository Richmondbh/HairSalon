namespace HairSalon.Api.Infrastructure;

// =====================================================================================
//  Custom exceptions used by the SERVICE LAYER.
//  A service simply throws, e.g.  throw new NotFoundException("Service not found.");
//  ApiExceptionHandler catches it and turns it into the right HTTP status code.
//  This keeps the controllers short - they never need try/catch.
// =====================================================================================

/// <summary>A business rule was broken (e.g. "That time is already booked"). -> 400 Bad Request</summary>
public class BusinessRuleException(string message) : Exception(message);

/// <summary>The requested item does not exist. -> 404 Not Found</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>The user is logged in but not allowed to do this. -> 403 Forbidden</summary>
public class ForbiddenException(string message) : Exception(message);
