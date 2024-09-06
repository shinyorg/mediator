namespace Http.TheActual;

[Cache(AbsoluteExpirationSeconds = 10)]
public partial class TestResultHttpRequest
{
    // note that the request handler is not present in the code
    // it is built into the mediator package
}