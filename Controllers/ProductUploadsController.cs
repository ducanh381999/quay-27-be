using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Products;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/products/uploads")]
public class ProductUploadsController : ControllerBase
{
    private readonly IProductUploadService _uploadService;

    public ProductUploadsController(IProductUploadService uploadService)
    {
        _uploadService = uploadService;
    }

    [HttpPost("images")]
    [ProducesResponseType(typeof(UploadedImageAssetResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<UploadedImageAssetResponse>> UploadImage(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
            return BadRequest(new { title = "Validation Failed", detail = "file is required." });

        await using var stream = file.OpenReadStream();
        var result = await _uploadService.UploadImageAsync(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }
}
