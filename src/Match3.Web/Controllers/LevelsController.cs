using Microsoft.AspNetCore.Mvc;
using Match3.Editor.Interfaces;
using Match3.Editor.ViewModels;
using Match3.Core.Analysis;

namespace Match3.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LevelsController : ControllerBase
{
    private readonly ILevelService _levelService;

    public LevelsController(ILevelService levelService)
    {
        _levelService = levelService;
    }

    [HttpGet("tree")]
    public ActionResult<ScenarioFolderNode> GetTree()
    {
        return Ok(_levelService.BuildTree());
    }

    [HttpGet("read")]
    public ActionResult<string> ReadLevel([FromQuery] string path)
    {
        try
        {
            var json = _levelService.ReadLevelJson(path);
            return Ok(json);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("write")]
    public ActionResult WriteLevel([FromQuery] string path, [FromBody] string json)
    {
        try
        {
            _levelService.WriteLevelJson(path, json);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("create")]
    public ActionResult<string> CreateLevel([FromQuery] string folder, [FromQuery] string name, [FromBody] string json)
    {
        try
        {
            var newPath = _levelService.CreateNewLevel(folder, name, json);
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
            var newPath = _levelService.CreateFolder(parent, name);
            return Ok(newPath);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("duplicate")]
    public ActionResult<string> DuplicateLevel([FromQuery] string source, [FromQuery] string newName)
    {
        try
        {
            var newPath = _levelService.DuplicateLevel(source, newName);
            return Ok(newPath);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("delete")]
    public ActionResult DeleteLevel([FromQuery] string path)
    {
        try
        {
            _levelService.DeleteLevel(path);
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
            _levelService.DeleteFolder(path);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("rename")]
    public ActionResult RenameLevel([FromQuery] string path, [FromQuery] string newName)
    {
        try
        {
            _levelService.RenameLevel(path, newName);
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
            _levelService.RenameFolder(path, newName);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("analysis")]
    public ActionResult<LevelAnalysisSnapshot?> GetAnalysis([FromQuery] string path)
    {
        var snapshot = _levelService.ReadAnalysisSnapshot(path);
        return Ok(snapshot);
    }

    [HttpPost("analysis")]
    public ActionResult WriteAnalysis([FromQuery] string path, [FromBody] LevelAnalysisSnapshot snapshot)
    {
        try
        {
            _levelService.WriteAnalysisSnapshot(path, snapshot);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
