﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyLittleProjectManager.Data;
using MyLittleProjectManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyLittleProjectManager.Controllers
{
	public class ProjectController : Controller
    {
		private readonly ApplicationDbContext _context;

		public ProjectController(ApplicationDbContext context)
		{
			_context = context;
		}

        public IActionResult Index()
        {
			var project = _context.Projects
				.Include(p => p.Columns)
					.ThenInclude(c => c.Cards)
				.FirstOrDefault();
            return View(project);
        }

		public JsonResult MoveCard(int CardId, int NewColumnId, string name = "", string description = "")
		{
			Console.WriteLine(String.Format("Moving card {0} to column {1}", CardId, NewColumnId));
            var profiles = _context.PlayerProfiles.ToList();
			var player = (_context.Users.Where(u => u.UserName == User.Identity.Name).FirstOrDefault()).PlayerProfile;

			Card movingCard;
			if (CardId == -1) // Create card
			{
				movingCard = new Card() { Name = name, Description = description };
				_context.Cards.Add(movingCard);

				player.MICoins += 5;
			}
			else // Move card
			{
				movingCard = _context.Cards.SingleOrDefault(c => c.Id == CardId);

				Column oldColumn = _context.Columns.Where(col => col.Cards.Any(card => card.Id == movingCard.Id)).SingleOrDefault();
				oldColumn.Cards.Remove(movingCard);
				foreach (var card in oldColumn.Cards) if (card.Order > movingCard.Order) card.Order--;

				player.MICoins += 3;
			}

			Column newColumn = _context.Columns.SingleOrDefault(c => c.Id == NewColumnId);
			movingCard.Order = (newColumn.Cards != null) ? newColumn.Cards.Count : 0;
			newColumn.Cards.Add(movingCard);

			_context.SaveChanges();

			return Json(new { oldId = CardId, newId = movingCard.Id });
        }

        public JsonResult DeleteCard(int CardId)
        {
            var profiles = _context.PlayerProfiles.ToList();
            Console.WriteLine(String.Format("Deleting card {0}", CardId));

			Card deletingCard = _context.Cards.SingleOrDefault(c => c.Id == CardId);
			Column column = _context.Columns.Where(c => c.Cards.Any(card => card.Id == deletingCard.Id)).SingleOrDefault();
			foreach (var card in column.Cards) if (card.Order > deletingCard.Order) card.Order--;

			var player = (_context.Users.Where(u => u.UserName == User.Identity.Name).FirstOrDefault()).PlayerProfile;
			player.MICoins += 1;

			_context.Cards.Remove(deletingCard);
			_context.SaveChanges();

			return Json(CardId);
		}

        public JsonResult CreateCard(string name,string description)
        {
            Card card = new Card()
            {
                Name = name,
                Description = description
            };
            return Json(card);
        }

        public List<Column> Columns(int projectId)
        {
			var tmp = _context.Projects.Include(c => c.Columns).ThenInclude(c => c.Cards).SingleOrDefault(p => p.Id == projectId);
			return tmp.Columns.ToList();
        }
    }
}