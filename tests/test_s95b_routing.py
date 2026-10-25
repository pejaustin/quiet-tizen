import pathlib
import re
import sqlite3
import tempfile
import unittest

RECIPE = pathlib.Path(__file__).resolve().parents[1] / 'recipes/s95b-settings-routing.sh'


class S95bRoutingTests(unittest.TestCase):
    def test_round_trip_and_apply_drift(self):
        scripts = re.findall(r'<<SQL\n(.*?)\nSQL', RECIPE.read_text(), re.S)
        self.assertEqual(len(scripts), 2)
        with tempfile.TemporaryDirectory() as directory:
            before = pathlib.Path(directory) / '.app-context.db'
            with sqlite3.connect(before) as c:
                c.execute('CREATE TABLE key_filter(appid,key,longpress,override,on_selector,request,target_selector,args)')
                c.execute("INSERT INTO key_filter VALUES('org.tizen.menu','XF86SysMenu',NULL,NULL,'','launch','package:org.tizen.menu','original')")
                c.execute("INSERT INTO key_filter VALUES('org.tizen.function-osd','XF86More',NULL,NULL,'','launch','numeric','keep')")
            live = pathlib.Path(directory) / 'live.db'
            live.write_bytes(before.read_bytes())
            def execute(index):
                with sqlite3.connect(live) as c:
                    c.executescript(scripts[index].replace('$D', directory))
            execute(0)
            with sqlite3.connect(live) as c:
                c.execute("UPDATE key_filter SET longpress='new metadata' WHERE key='XF86SysMenu'")
                self.assertEqual(c.execute("SELECT target_selector FROM key_filter WHERE key='XF86More'").fetchone()[0], 'numeric')
            execute(1)
            with sqlite3.connect(live) as c:
                self.assertEqual(c.execute("SELECT target_selector,args,longpress FROM key_filter WHERE key='XF86SysMenu'").fetchone(), ('package:org.tizen.menu','original','new metadata'))
            with self.assertRaises(sqlite3.IntegrityError):
                execute(0)
            # A new route must not be overwritten by restore.
            live.write_bytes(before.read_bytes())
            execute(0)
            with sqlite3.connect(live) as c:
                c.execute("UPDATE key_filter SET target_selector='new route' WHERE key='XF86SysMenu'")
            with self.assertRaises(sqlite3.IntegrityError):
                execute(1)
