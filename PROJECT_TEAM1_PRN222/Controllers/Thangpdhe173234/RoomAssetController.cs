using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;

public class RoomAssetController : Controller
{
    private readonly AppDbContext _context; // Đảm bảo khớp với tên file Context của bạn
    private readonly IWebHostEnvironment _hostEnvironment;

    public RoomAssetController(AppDbContext context, IWebHostEnvironment hostEnvironment)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoomAsset asset, IFormFile? AssetFile)
    {
        // Kiểm tra xem RoomId có hợp lệ không trước khi lưu
        if (asset.RoomId == Guid.Empty)
        {
            return BadRequest("Không tìm thấy thông tin phòng.");
        }

        if (AssetFile != null && AssetFile.Length > 0)
        {
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AssetFile.FileName);
            string uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "assets");

            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            string filePath = Path.Combine(uploadDir, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await AssetFile.CopyToAsync(fileStream);
            }

            asset.ImageUrl = "/uploads/assets/" + fileName;
        }

        asset.Id = Guid.NewGuid();
        // Dùng InstalledDate như model bạn đã cập nhật
        asset.InstalledDate = DateTime.Now;

        _context.RoomAssets.Add(asset);
        await _context.SaveChangesAsync();

        // Quay lại trang chi tiết phòng
        return RedirectToAction("Details", "Room", new { id = asset.RoomId });
    }

    [HttpPost]
    // Xóa qua Ajax thì không cần ValidateAntiForgeryToken nếu không cấu hình Header, 
    // nhưng tốt nhất nên dùng để bảo mật.
    public async Task<IActionResult> Delete(Guid id)
    {
        var asset = await _context.RoomAssets.FindAsync(id);
        if (asset == null) return NotFound();

        // Xóa file vật lý trước khi xóa bản ghi
        if (!string.IsNullOrEmpty(asset.ImageUrl))
        {
            string oldFilePath = Path.Combine(_hostEnvironment.WebRootPath, asset.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(oldFilePath))
            {
                System.IO.File.Delete(oldFilePath);
            }
        }

        _context.RoomAssets.Remove(asset);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost]
    public IActionResult DeleteAsset(Guid id)
    {
        var asset = _context.RoomAssets.Find(id);
        if (asset == null) return Json(new { success = false, message = "Không tìm thấy tài sản" });

        _context.RoomAssets.Remove(asset);
        _context.SaveChanges();

        return Json(new { success = true });
    }
}