using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using StoreAPI.Models;
using System.Web.Security;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace StoreAPI.Controllers
{
    public class CustomersController : Controller
    {
        private StoreContext db = new StoreContext();

        [HttpGet]
        [Authorize(Roles = "admin")]
        public string IndexJson()
        {

            Customer customer = db.Customers.Where(c => c.login == User.Identity.Name).FirstOrDefault();

            ID id = new ID();

            id.id_customer = customer.id_customer;

            string json = JsonConvert.SerializeObject(id);

            return json;
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string login, string password)
        {
            if (login == "" || password == "")
            {
                ViewBag.error = "Заполните все поля";
                return View();
            }

            password = Encode(password);

            Customer customer =  db.Customers.FirstOrDefault(u => u.login == login && u.password == password);

            
            if (customer != null)
            {
                FormsAuthentication.SetAuthCookie(login, true);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.error = "Такого пользователя не существует";
                return View();
            }
            
        }

        [HttpPost]
        [Route("api/[controller]")]
        public string LoginJson(Account model)
        {
            var ID = new ID();

            model.password = Encode(model.password);

            Customer customer =  db.Customers.FirstOrDefault(u => u.login == model.login);

            if (customer != null)
            {
                Customer customer_base = db.Customers.FirstOrDefault(u => u.login == model.login && u.password == model.password);
                if (customer_base != null)
                {
                    FormsAuthentication.SetAuthCookie(model.login, true);

                    ID.id_customer = customer_base.id_customer;
                    string json = JsonConvert.SerializeObject(ID, Formatting.Indented);
                    return json;
                }
                else 
                {
                    ID.id_customer = 0;
                    string json = JsonConvert.SerializeObject(ID, Formatting.Indented);
                    return json;
                }
            }
            else
            {
                ID.id_customer = 0;
                string json = JsonConvert.SerializeObject(ID, Formatting.Indented);
                return json;
            }

        }

        [Authorize(Roles = "admin,user")]
        [HttpPost]
        [Route("api/[controller]")]
        public string AccountJson(ID model)
        {
            Customer customer = db.Customers.FirstOrDefault(i => i.id_customer == model.id_customer);
            Profile profile = new Profile();

            profile.id_customer = customer.id_customer;
            profile.login = customer.login;
            profile.phone = customer.phone;
            profile.adress_customer = customer.adress_customer;

            string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            return json;
        }

        [Authorize(Roles = "admin,user")]
        [HttpPost]
        [Route("api/[controller]")]
        public string EditAccountJson(Profile model)
        {
            Customer customer = new Customer();
            Customer customerEdit = new Customer();

            if (model != null) customer = db.Customers.FirstOrDefault(i => i.id_customer == model.id_customer);

            if (customer != null)
            {
                customerEdit = db.Customers.FirstOrDefault(i => i.id_customer == model.id_customer);

                customerEdit.login = customer.login;
                customerEdit.password = customer.password;
                customerEdit.phone = model.phone;
                customerEdit.adress_customer = model.adress_customer;
                customerEdit.roleid = 2;
                customerEdit.role = null;

                db.Entry(customerEdit).State = EntityState.Modified;
                db.SaveChangesAsync();

                string json = JsonConvert.SerializeObject(customerEdit, Formatting.Indented);
                return json;
            }
            return "Пользователя не существует";

        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public string StatusJson()
        {
            ID id = new ID();

            id.id_customer = 0;

            string json = JsonConvert.SerializeObject(id);

            return json;
        }


        [Authorize(Roles = "admin")]
        public ActionResult Register()
        {
            return View();
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register([Bind(Include = "id_customer,login,password")] CustomerCustom modelmodified)
        {

            Customer model = new Customer();

            model.login = modelmodified.login;
            model.password = Encode(modelmodified.password);
            model.phone = "-";
            model.adress_customer = "-";
            model.roleid = 1;

            if (model.login != null && model.password != null)
            {
                Customer customer = db.Customers.FirstOrDefault(u => u.login == model.login);

                if (customer == null)
                {

                    

                    db.Customers.Add(model);
                    db.SaveChanges();

                    customer = db.Customers.Where(u => u.login == model.login && u.password == model.password).FirstOrDefault();

                    // если пользователь удачно добавлен в бд
                    if (customer != null)
                    {
                        FormsAuthentication.SetAuthCookie(model.login, true);
                        return RedirectToAction("Index", "Customers");
                    }
                }
                else
                {
                    if (model.login != null ) ViewBag.login = "Требуется поле логин";
                    if (model.password != null) ViewBag.password = "Требуется поле пароль";
                    return View();
                }
            }

            return View(model);
        }

        [HttpPost]
        [Route("api/[controller]")]
        public string RegisterJson(CustomerCustom model)
        {
            ID answer = new ID();
            answer.id_customer = 0;
            string jsonDefault = JsonConvert.SerializeObject(answer);

            if (model.login == null) return jsonDefault;
            if (model.password == null) return jsonDefault;
            if (model.phone == null) return jsonDefault;
            if (model.phone.Length < 11) return jsonDefault;
            if (model.adress_customer == null) return jsonDefault;

            Customer customer = new Customer
            {
                id_customer = db.Customers.ToList().LastOrDefault().id_customer + 1,
                login = model.login,
                password = Encode(model.password),
                phone = model.phone,
                adress_customer = model.adress_customer,
                roleid = 2,
                role = null,
                requests = new List<Request>()
            };


            if (db.Customers.Where(u => u.login == customer.login).FirstOrDefault() == null)
            {
                db.Customers.Add(customer);
                db.SaveChanges();
            }
            else
            {
                answer.id_customer = -1;
                string json= JsonConvert.SerializeObject(answer);
                return json;
            }
            Customer customer_db = db.Customers.Where(u => u.login == customer.login && u.password == customer.password).FirstOrDefault();

            if (customer_db != null)
            {
                answer.id_customer = customer.id_customer;
                string json = JsonConvert.SerializeObject(answer);
                return json;
            }
            else
            {
                answer.id_customer = -1;
                string json = JsonConvert.SerializeObject(answer);
                return json;
            }

        }

        public ActionResult Logoff()
        {
            if (User.Identity.IsAuthenticated)
            {
                FormsAuthentication.SignOut();
                return RedirectToAction("Index", "Home");
            }
            else 
            {
                ViewBag.account = "Войти";
                return RedirectToAction("Login", "Customers");
            }
        }

        public string LogoffJson()
        {
            var ID = new AccountStatus();

            if (User.Identity.IsAuthenticated)
            {
                FormsAuthentication.SignOut();
                ID.status = "Успешный выход";
                string json = JsonConvert.SerializeObject(ID, Formatting.Indented);
                return json;
            }
            else
            {
                ID.status = "Пользователь не авторизован";
                string json = JsonConvert.SerializeObject(ID, Formatting.Indented);
                return json;
            }
        }


        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Index()
        {
            ViewBag.account = User.Identity.Name;

            return View(await db.Customers.Include(r=>r.role).ToListAsync());
        }

        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Account()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Customer customer = await db.Customers.Where(c => c.login == User.Identity.Name).FirstOrDefaultAsync();
            if (customer == null)
            {
                return HttpNotFound();
            }
            return View(customer);
        }


        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Customer customer = await db.Customers.FindAsync(id);
            if (customer == null)
            {
                return HttpNotFound();
            }
            return View(customer);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "id_customer,login,password,phone,adress_customer")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                db.Entry(customer).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(customer);
        }

        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Customer customer = await db.Customers.FindAsync(id);
            if (customer == null)
            {
                return HttpNotFound();
            }
            return View(customer);
        }

        [Authorize(Roles = "admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Customer customer = await db.Customers.FindAsync(id);
            db.Customers.Remove(customer);
            await db.SaveChangesAsync();
            return RedirectToAction("Index", "Customers");
        }

        public string Encode(string input)
        {

            byte[] hash = Encoding.ASCII.GetBytes(input);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] hashenc = md5.ComputeHash(hash);
            string result = "";
            foreach (var b in hashenc)
            {
                result += b.ToString("x2");
            }
            return result;

        }


        [Authorize(Roles = "admin")]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
