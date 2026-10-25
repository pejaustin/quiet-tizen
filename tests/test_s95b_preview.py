"""A successful process exit alone must not pass a live root trial."""
import copy
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))
from s95b_preview import root_trial_succeeded


class S95bPreviewTests(unittest.TestCase):
    def test_requires_identity_command_and_cleanup_evidence(self):
        good = {"authenticated": True, "uid": 0, "euid": 0,
                "staging_cleaned": True, "system_files_unchanged": True,
                "commands": [{"exit_code": 0, "timed_out": False}]}
        self.assertTrue(root_trial_succeeded(good))
        for key in good:
            altered = copy.deepcopy(good)
            del altered[key]
            self.assertFalse(root_trial_succeeded(altered), key)
        for command in [[], [{"exit_code": 139, "timed_out": False}],
                        [{"exit_code": 0, "timed_out": True}]]:
            self.assertFalse(root_trial_succeeded({**good, "commands": command}))
        self.assertFalse(root_trial_succeeded({**good, "uid": 901}))


if __name__ == "__main__":
    unittest.main()
