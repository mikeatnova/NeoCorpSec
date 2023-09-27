namespace NeoCorpSec.Models.CameraManagement
{
    public class UpdateCameraStatusRequest
    {
        public int CameraId { get; set; }
        public string NewStatus { get; set; }
    }
}
