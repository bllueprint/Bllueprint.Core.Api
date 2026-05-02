using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bllueprint.Core.Api.Tests;

public class ControllerFixture
{
    public ControllerFixture()
    {
        Controller = new FakeController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    public ControllerBase Controller { get; }

    private sealed class FakeController : ControllerBase;
}
