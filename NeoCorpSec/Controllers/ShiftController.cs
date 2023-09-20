using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeoCorpSec.Models.ShiftManagement;
using NeoCorpSec.Services;
using System.Security.Claims;

namespace NeoCorpSec.Controllers
{
    public class ShiftController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        // Inject any services or repositories as needed

        // Starts a new shift
        // PUT api/Shift/Start/{id}
        [HttpPut("Start/{id}")]
        public IActionResult StartShift(int id)
        {
            // Find logged in user by id
            // Create a new guid for shift
            // StartTime = datetime now
            // Status = In Progress
            // ToursCompleted = 0
            // create the object 
            // send the object to NeoApi api/post/shift
            return Ok();  // Replace with proper response
        }

        // Ends an existing shift
        // PUT api/Shift/End/{id}
        [HttpPut("End/{id}")]
        public IActionResult EndShift(int id)
        {
            // Implement logic to end the shift and log end time
            // Again, consider updating the state in the database
            // Return updated shift state
            return Ok();  // Replace with proper response
        }
    }
}