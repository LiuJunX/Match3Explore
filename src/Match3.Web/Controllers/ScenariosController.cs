using Microsoft.AspNetCore.Mvc;
using Match3.Editor.Interfaces;
using Match3.Editor.ViewModels;

namespace Match3.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScenariosController : ControllerBase
{
    private readonly IScenarioService _scenarioService;

    public ScenariosController(IScenarioService scenarioService)
    {
        _scenarioService = scenarioService;
    }

    [HttpGet("tree")]
    public ActionResult<ScenarioFolderNode> GetTree()
    {
        return Ok(_scenarioService.BuildTree());
    }

    [HttpGet("read")]
    public ActionResult<string> ReadScenario([FromQuery] string path)
    {
        try
        {
            var json = _scenarioService.ReadScenarioJson(path);
            return Ok(json);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("write")]
    public ActionResult WriteScenario([FromQuery] string path, [FromBody] string json)
    {
        try
        {
            _scenarioService.WriteScenarioJson(path, json);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("create")]
    public ActionResult<string> CreateScenario([FromQuery] string folder, [FromQuery] string name, [FromBody] string json)
    {
        try
        {
            var newPath = _scenarioService.CreateNewScenario(folder, name, json);
            return Ok(newPath);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("create-folder")]
    public ActionResult<string> CreateFolder([FromQuery] string parent, [FromQuery] string name)
    {
        try
        {
            var newPath = _scenarioService.CreateFolder(parent, name);
            return Ok(newPath);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("duplicate")]
    public ActionResult<string> DuplicateScenario([FromQuery] string source, [FromQuery] string newName)
    {
        try
        {
            var newPath = _scenarioService.DuplicateScenario(source, newName);
            return Ok(newPath);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("delete")]
    public ActionResult DeleteScenario([FromQuery] string path)
    {
        try
        {
            _scenarioService.DeleteScenario(path);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("delete-folder")]
    public ActionResult DeleteFolder([FromQuery] string path)
    {
        try
        {
            _scenarioService.DeleteFolder(path);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("rename")]
    public ActionResult RenameScenario([FromQuery] string path, [FromQuery] string newName)
    {
        try
        {
            _scenarioService.RenameScenario(path, newName);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("rename-folder")]
    public ActionResult RenameFolder([FromQuery] string path, [FromQuery] string newName)
    {
        try
        {
            _scenarioService.RenameFolder(path, newName);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
