using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Gala;
using Gala.Linq.Sql;
using Gala.Config;

namespace gzpt
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        public class BatchTest : Obj
        {
            public string Name { get; set; }
            public int Sex { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Unnamed1_Click(object sender, EventArgs e)
        {
            List<BatchTest> list = new List<BatchTest>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(new BatchTest() { Name = i.ToString(), Sex = 0 });
            }

            ObjQuery<BatchTest> q = new ObjQuery<BatchTest>(ConfigBase.GetConnectionStringSettings("gzpt"));
            q.Batch = true;

            q.AddRange(list);
            q.SubmitChanges();
        }

        protected void Unnamed2_Click(object sender, EventArgs e)
        {
            ObjQuery<BatchTest> q = new ObjQuery<BatchTest>(ConfigBase.GetConnectionStringSettings("gzpt"));
            q.Batch = true;

            List<BatchTest> list = (from l in q
                                    select l).ToList();

            foreach (var item in list)
            {
                item.Sex = 1;
            }

            q.SubmitChanges();
        }

        protected void Unnamed3_Click(object sender, EventArgs e)
        {
            ObjQuery<BatchTest> q = new ObjQuery<BatchTest>(ConfigBase.GetConnectionStringSettings("gzpt"));
            q.Batch = true;

            List<BatchTest> list = (from l in q
                                    select l).ToList();

            foreach (var item in list)
            {
                q.Remove(item);
            }

            q.SubmitChanges();
        }
    }
}
