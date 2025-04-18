﻿using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SourceGit.ViewModels
{
    public class CreateBranch : Popup
    {
        [Required(ErrorMessage = "Branch name is required!")]
        [RegularExpression(@"^[\w \-/\.#\+]+$", ErrorMessage = "Bad branch name format!")]
        [CustomValidation(typeof(CreateBranch), nameof(ValidateBranchName))]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value, true);
        }

        public object BasedOn
        {
            get;
        }

        public bool DiscardLocalChanges
        {
            get;
            set;
        }

        public bool CheckoutAfterCreated
        {
            get => _repo.Settings.CheckoutBranchOnCreateBranch;
            set => _repo.Settings.CheckoutBranchOnCreateBranch = value;
        }

        public bool IsBareRepository
        {
            get => _repo.IsBare;
        }

        public CreateBranch(Repository repo, Models.Branch branch)
        {
            _repo = repo;
            _baseOnRevision = branch.IsDetachedHead ? branch.Head : branch.FullName;

            if (!branch.IsLocal && repo.Branches.Find(x => x.IsLocal && x.Name == branch.Name) == null)
            {
                Name = branch.Name;
            }

            BasedOn = branch;
            DiscardLocalChanges = false;
            View = new Views.CreateBranch() { DataContext = this };
        }

        public CreateBranch(Repository repo, Models.Commit commit)
        {
            _repo = repo;
            _baseOnRevision = commit.SHA;

            BasedOn = commit;
            DiscardLocalChanges = false;
            View = new Views.CreateBranch() { DataContext = this };
        }

        public CreateBranch(Repository repo, Models.Tag tag)
        {
            _repo = repo;
            _baseOnRevision = tag.SHA;

            BasedOn = tag;
            DiscardLocalChanges = false;
            View = new Views.CreateBranch() { DataContext = this };
        }

        public static ValidationResult ValidateBranchName(string name, ValidationContext ctx)
        {
            var creator = ctx.ObjectInstance as CreateBranch;
            if (creator == null)
                return new ValidationResult("Missing runtime context to create branch!");

            var fixedName = creator.FixName(name);
            foreach (var b in creator._repo.Branches)
            {
                if (b.FriendlyName == fixedName)
                    return new ValidationResult("A branch with same name already exists!");
            }

            return ValidationResult.Success;
        }

        public override Task<bool> Sure()
        {
            _repo.SetWatcherEnabled(false);

            var fixedName = FixName(_name);
            return Task.Run(() =>
            {
                var succ = false;
                if (CheckoutAfterCreated && !_repo.IsBare)
                {
                    var changes = new Commands.CountLocalChangesWithoutUntracked(_repo.FullPath).Result();
                    var needPopStash = false;
                    if (changes > 0)
                    {
                        if (DiscardLocalChanges)
                        {
                            SetProgressDescription("Discard local changes...");
                            Commands.Discard.All(_repo.FullPath, false);
                        }
                        else
                        {
                            SetProgressDescription("Stash local changes");
                            succ = new Commands.Stash(_repo.FullPath).Push("CREATE_BRANCH_AUTO_STASH");
                            if (!succ)
                            {
                                CallUIThread(() => _repo.SetWatcherEnabled(true));
                                return false;
                            }

                            needPopStash = true;
                        }
                    }

                    SetProgressDescription($"Create new branch '{fixedName}'");
                    succ = new Commands.Checkout(_repo.FullPath).Branch(fixedName, _baseOnRevision, SetProgressDescription);

                    if (needPopStash)
                    {
                        SetProgressDescription("Re-apply local changes...");
                        new Commands.Stash(_repo.FullPath).Pop("stash@{0}");
                    }
                }
                else
                {
                    SetProgressDescription($"Create new branch '{fixedName}'");
                    succ = Commands.Branch.Create(_repo.FullPath, fixedName, _baseOnRevision);
                }

                CallUIThread(() =>
                {
                    if (succ && CheckoutAfterCreated)
                    {
                        var fake = new Models.Branch() { IsLocal = true, FullName = $"refs/heads/{fixedName}" };
                        if (BasedOn is Models.Branch based && !based.IsLocal)
                            fake.Upstream = based.FullName;

                        var folderEndIdx = fake.FullName.LastIndexOf('/');
                        if (folderEndIdx > 10)
                            _repo.Settings.ExpandedBranchNodesInSideBar.Add(fake.FullName.Substring(0, folderEndIdx));

                        if (_repo.HistoriesFilterMode == Models.FilterMode.Included)
                            _repo.SetBranchFilterMode(fake, Models.FilterMode.Included, true, false);
                    }

                    _repo.MarkBranchesDirtyManually();
                    _repo.SetWatcherEnabled(true);
                });

                return true;
            });
        }

        private string FixName(string name)
        {
            if (!name.Contains(' '))
                return name;

            var parts = name.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            return string.Join("-", parts);
        }

        private readonly Repository _repo = null;
        private string _name = null;
        private readonly string _baseOnRevision = null;
    }
}
