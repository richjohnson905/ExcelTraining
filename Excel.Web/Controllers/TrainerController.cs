﻿using Excel.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Excel.Web.Controllers
{
    public class TrainerController : Controller
    {
        private IAthleteRepository athleteRepository;
        private QuickHelper helper = new QuickHelper();
      
        public TrainerController()
        {
            athleteRepository = new AthleteRepository();
            //GetUserId = () => User.Identity.GetUserId();
        }

        public TrainerController(IAthleteRepository db)
        {
            athleteRepository = db;
        }

        // GET: Trainer
        public ActionResult Index()
        {
            TrainerQueueViewModel model = new TrainerQueueViewModel();

            DateTime nextSession = helper.GetNextSession();
            model.SessionDate = nextSession.ToLongDateString();
            model.SessionTime = helper.GetSessionTimeString(nextSession);
            int locationId = helper.GetDardenne(athleteRepository).Id;
            Excel.Entities.Session session = helper.GetOrCreateSession(nextSession.Hour, nextSession, locationId, athleteRepository);
            model.PersonalAthletes = athleteRepository.GetPersonalTrainingAthletes(session.Id, locationId).ToList();
            model.SportsAthletes = athleteRepository.GetSportsTrainingAthletes(session.Id, locationId).ToList();
            LoadPersonalTrainerSelectList(model);
            LoadSportsTrainerSelectList(model);
            return View(model);
        }

        private void LoadPersonalTrainerSelectList(TrainerQueueViewModel model)
        {
            model.PersonalTrainerSelectList = new SelectList(athleteRepository.GetAllTrainers(), "Id", "LastName", model.PersonalTrainerId);
        }

        private void LoadSportsTrainerSelectList(TrainerQueueViewModel model)
        {
            model.SportsTrainerSelectList = new SelectList(athleteRepository.GetAllTrainers(), "Id", "LastName", model.SportsTrainerId);
        }
    }
}