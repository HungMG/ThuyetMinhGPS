using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideAdmin.Models;

namespace TourGuideAdmin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class POIsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public POIsApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/POIsApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<POI>>> GetPOIs()
        {
            // 🌟 MÀNG LỌC ĐA TẦNG: Chỉ trả về những điểm ĐÃ ĐƯỢC DUYỆT (ApprovalStatus == 1)
            var approvedPOIs = await _context.POIs
                                             .Where(p => p.ApprovalStatus == 1)
                                             .ToListAsync();

            return approvedPOIs;
        }

        // GET: api/POIsApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<POI>> GetPOI(int id)
        {
            var pOI = await _context.POIs.FindAsync(id);

            if (pOI == null)
            {
                return NotFound();
            }

            return pOI;
        }

        // PUT: api/POIsApi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPOI(int id, POI pOI)
        {
            if (id != pOI.Id)
            {
                return BadRequest();
            }

            _context.Entry(pOI).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!POIExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/POIsApi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<POI>> PostPOI(POI pOI)
        {
            _context.POIs.Add(pOI);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPOI", new { id = pOI.Id }, pOI);
        }

        // DELETE: api/POIsApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePOI(int id)
        {
            var pOI = await _context.POIs.FindAsync(id);
            if (pOI == null)
            {
                return NotFound();
            }

            _context.POIs.Remove(pOI);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool POIExists(int id)
        {
            return _context.POIs.Any(e => e.Id == id);
        }
    }
}
