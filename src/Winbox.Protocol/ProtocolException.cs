using System;

namespace Winbox.Protocol;

public sealed class ProtocolException(string message) : Exception(message);
