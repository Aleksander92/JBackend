using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using System.Net;
using Backend.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Backend.Controllers
{
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly Context Context;
        private static Mutex Mutex = new Mutex();

        private (string, string)[] Hiragana = {
            ("あ", "a"), ("い", "i"), ("う", "u"), ("え", "e"), ("お", "o"),
            ("か", "ka"), ("き", "ki"), ("く", "ku"), ("け", "ke"), ("こ", "ko"),
            ("さ", "sa"), ("し", "shi"), ("す", "su"), ("せ", "se"), ("そ", "so"),
            ("た", "ta"), ("ち", "chi"), ("つ", "tsu"), ("て", "te"), ("と", "to"),
            ("な", "na"), ("に", "ni"), ("ぬ", "nu"), ("ね", "ne"), ("の", "no"),
            ("は", "ha"), ("ひ", "hi"), ("ふ", "fu"), ("へ", "he"), ("ほ", "ho"),
            ("ま", "ma"), ("み", "mi"), ("む", "mu"), ("め", "me"), ("も", "mo"),
            ("や", "ya"), ("ゆ", "yu"), ("よ", "yo"), ("ら", "ra"), ("り", "ri"),
            ("る", "ru"), ("れ", "re"), ("ろ", "ro"), ("わ", "wa"), ("を", "wo"),
            ("ん", "n")
        };

        private int GenerateNewId() {
            Random random = new Random();
            while (true) {
                int id = random.Next();
                if (Context.States.Find(id) == null) {
                    return id;
                }
            }
        }

        private (string, string) GenerateTask(ModeState.EMode mode) {
            if ((ModeState.EMode)mode == ModeState.EMode.EModeEnglishToHiragana) {
                Random random = new Random();
                int taskInd = random.Next(Hiragana.Length);
                var task = Hiragana[taskInd];
                (task.Item1, task.Item2) = (task.Item2, task.Item1);
                return task;
            } else {
                throw new NotImplementedException();
            }
        }

        private (string, string) GenerateTask(int mode) {
            return GenerateTask((ModeState.EMode)mode);
        }

        private async Task<ModeState> CreateState(int mode) {
            ModeState state;

            try {
                Mutex.WaitOne();

                int id = GenerateNewId();
                (string, string) task = GenerateTask(mode);
                state = new ModeState(id, mode, task);

                Context.States.Add(state);
                await Context.SaveChangesAsync();
            } catch {
                throw;
            } finally {
                Mutex.ReleaseMutex();
            }

            return state;
        }

        public GameController(Context context) {
            Context = context;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        //                                            API
        //////////////////////////////////////////////////////////////////////////////////////////////

        [HttpGet]
        [AllowCrossSite]
        [Route("states")]
        public async Task<ActionResult<IEnumerable<ModeState>>> GetStates() {
            if (Context.States == null)
            {
                return NotFound();
            }
            return await Context.States.ToListAsync();
        }

        [HttpGet]
        [AllowCrossSite]
        [Route("get_state")]
        public async Task<ActionResult<ModeState>> GetState(int id) {
            if (Context.States == null) {
                return NotFound();
            }

            var state = await Context.States.FindAsync(id);
            if (state == null) {
                return NotFound();
            }

            return state;
        }

        [HttpGet]
        [AllowCrossSite]
        [Route("check_answer_and_make_new_task")]
        public async Task<ActionResult<ModeState>> CheckAnswerAndMakeNewTask(int id, string userAnswer) {
            if (Context.States == null) {
                return NotFound();
            }

            var state = await Context.States.FindAsync(id);
            if (state == null) {
                return NotFound();
            }

            ++state.Attempts;
            if (state.TaskAnswer == userAnswer) {
                ++state.CorrectAttempts;
            }
            state.SetTask(GenerateTask(state.Mode));

            Context.States.Update(state);
            await Context.SaveChangesAsync();

            state.TaskAnswer = "";

            return state;
        }

        [HttpGet]
        [AllowCrossSite]
        [Route("create_game")]
        public async Task<ActionResult<ModeState>> CreateGame(int mode) {
            if (Context.States == null)
            {
                return Problem("Entity set 'Context.States' is null.");
            }

            ModeState state;
            try {
                state = await CreateState(mode);
                state.TaskAnswer = "";
            } catch (Exception e) {
                return Problem($"CreateGame failed: {e.Message}");
            } 

            return CreatedAtAction(nameof(CreateGame), new { id = state.Id }, state);
        }

        //// PUT: api/States/5
        //// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutState(long id, State state)
        //{
        //    if (id != state.Id)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(state).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!StateExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return NoContent();
        //}

        //// POST: api/States
        //// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPost]
        //public async Task<ActionResult<State>> PostState(State state) {
        //    if (_context.States == null) {
        //        return Problem("Entity set 'Context.States' is null.");
        //    }

        //    _context.States.Add(state);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetState), new { id = state.Id }, state);
        //}

        //// DELETE: api/States/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteState(long id)
        //{
        //    if (_context.States == null)
        //    {
        //        return NotFound();
        //    }
        //    var state = await _context.States.FindAsync(id);
        //    if (state == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.States.Remove(state);
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}

        //private bool StateExists(long id)
        //{
        //    return (_context.States?.Any(e => e.Id == id)).GetValueOrDefault();
        //}        //// PUT: api/States/5
        //// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutState(long id, State state)
        //{
        //    if (id != state.Id)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(state).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!StateExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return NoContent();
        //}

        //// POST: api/States
        //// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPost]
        //public async Task<ActionResult<State>> PostState(State state) {
        //    if (_context.States == null) {
        //        return Problem("Entity set 'Context.States' is null.");
        //    }

        //    _context.States.Add(state);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetState), new { id = state.Id }, state);
        //}

        //// DELETE: api/States/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteState(long id)
        //{
        //    if (_context.States == null)
        //    {
        //        return NotFound();
        //    }
        //    var state = await _context.States.FindAsync(id);
        //    if (state == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.States.Remove(state);
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}

        //private bool StateExists(long id)
        //{
        //    return (_context.States?.Any(e => e.Id == id)).GetValueOrDefault();
        //}
    }
}
