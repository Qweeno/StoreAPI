using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using StoreAPI.Models;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace StoreAPI.Controllers
{
    public class RequestsController : Controller
    {
        private StoreContext db = new StoreContext();

        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Index()
        {
            ViewBag.account = User.Identity.Name; 

            var requests = db.Requests.Include(c => c.customer);

            return View(await requests.ToListAsync());
        }

        [HttpPost]
        [Route("api/[controller]")]
        [Authorize(Roles = "admin,user")]
        public String CreateJson(RequestCustom model)
        {

            var requestAnswer = new RequestCancel();

            if (db.Customers.Where(u => u.id_customer == model.id_customer).FirstOrDefault() != null) 
            {

                if (db.Types.SingleOrDefault(i => i.id_type_delivery == model.id_type_delivery) == null) 
                {
                    requestAnswer.id_request = 0;
                    string json = JsonConvert.SerializeObject(requestAnswer, Formatting.Indented);
                    return json;
                 }

                Request request = new Request();

                List<Request> requests_list = db.Requests.ToList();

                if (requests_list.Where(r => r.id_request == 1).FirstOrDefault() == null) request.id_request = 1;
                else request.id_request = requests_list.LastOrDefault().id_request + 1;

                //Неподтверждённый заказ без товаров.
                request.date_request = DateTime.Now;
                request.date_confirm = DateTime.Now;
                request.date_delivery = DateTime.Now;
                request.id_customer = model.id_customer;
                request.status = 1;
                request.cost_request = 0;
                request.id_type_delivery = model.id_type_delivery;
                request.type = null;

                if (db.Requests.Where(u => u.id_request == request.id_request).FirstOrDefault() == null)
                {
                    db.Requests.Add(request);
                    db.SaveChanges();
                }
                else
                {
                    requestAnswer.id_request = 0;
                    string json = JsonConvert.SerializeObject(requestAnswer, Formatting.Indented);
                    return json;
                }

                Request request_db = db.Requests.Where(u => u.id_request == request.id_request).FirstOrDefault();

                if (request_db != null) 
                {
                    requestAnswer.id_request = request.id_request;
                    string json = JsonConvert.SerializeObject(requestAnswer, Formatting.Indented);
                    return json;
                }

                else 
                {
                    requestAnswer.id_request = 0;
                    string json = JsonConvert.SerializeObject(requestAnswer, Formatting.Indented);
                    return json;
                }

            }

            requestAnswer.id_request = 0;
            return JsonConvert.SerializeObject(requestAnswer, Formatting.Indented);

        }

        [HttpPost]
        [Route("api/[controller]")]
        [Authorize(Roles = "admin,user")]
        public string AddProductJson(Product_request_custom model)
        {
            string json = JsonConvert.SerializeObject(model, Formatting.Indented);

            if (db.Requests.SingleOrDefault(i => i.id_request == model.id_request) == null) return json;
            if (db.Products.SingleOrDefault(i => i.id_product == model.id_product) == null) return json;

            var product_Request_list = db.Product_requests.ToList();

            Product_request product_Request = new Product_request();

            product_Request.id_request = model.id_request;

            if (product_Request_list.Where(p => p.id_product_request == 1).FirstOrDefault() == null) product_Request.id_product_request = 1;
            else product_Request.id_product_request = product_Request_list.LastOrDefault().id_product_request + 1;

            product_Request.count = model.count;

            product_Request.id_product = model.id_product;

            if (db.Product_requests.Where(u => u.id_product_request == product_Request.id_product_request).FirstOrDefault() == null)
            {

                db.Product_requests.Add(product_Request);
                db.SaveChanges();

                if (db.Products.Where(p => p.id_product == product_Request.id_product).FirstOrDefault() != null)
                {
                    return json;
                }
                return json;
            }
            else return json;
        }

        

        [HttpPost]
        [Route("api/[controller]")]
        [Authorize(Roles = "admin,user")]
        public string AllRequest(RequestCustomer model)
        {
            if (db.Requests.Where(r => r.id_customer == model.id_customer).ToList() != null && model != null)
            {
                var requests = db.Requests.Where(r => r.id_customer == model.id_customer).ToList();

                var json = JsonConvert.SerializeObject(requests, Formatting.Indented);

                return json;
            }
            else return "Нет ни одного заказа";
        }

        [HttpPost]
        [Route("api/[controller]")]
        [Authorize(Roles = "admin,user")]
        public string GetRequest(RequestCancel model)
        {
            if (db.Requests.Where(r => r.id_request == model.id_request).FirstOrDefault() != null && model != null)
            {
                var requests = db.Requests.Where(r => r.id_request == model.id_request).FirstOrDefault();

                var json = JsonConvert.SerializeObject(requests, Formatting.Indented);

                return json;
            }
            else return "Заказа не существует";
        }

        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Request request = await db.Requests.Include(r => r.product_requests).Include(t=>t.type).Include(r => r.customer).SingleOrDefaultAsync(r => r.id_request == id);

            if (request == null)
            {
                return HttpNotFound();
            }
            return View(request);
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<ActionResult> ConfirmDetails(int? id)
        {
            ViewBag.date = DateTime.Now;

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Request request = await db.Requests.Include(r => r.product_requests).Include(r => r.customer).SingleOrDefaultAsync(r => r.id_request == id);
            if (request == null)
            {
                return HttpNotFound();
            }

            if (request.product_requests.Count() == 0) ViewBag.error = "В заказе нет товаров";

            foreach (var item in request.product_requests)
            {
                var product = await db.Product_storage.Where(i => i.id_product == item.id_product).Include(p=>p.product).SingleOrDefaultAsync();

                if(product == null) ViewBag.error = "На складе не найдены товары";
                else if (product.count < item.count)
                {
                    ViewBag.error = @" Товар "" " + product.product.name_product + @" "" не в наличии. " + "Не хватает: " + (item.count-product.count).ToString();
                }
            }

            return View(request);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult> Confirm(int? id)
        {

            var request = await db.Requests.Include(r => r.product_requests).SingleOrDefaultAsync(r => r.id_request == id);

            var request_db = await db.Requests.FindAsync(id);

            if (request.product_requests.Count() != 0)
            {

                foreach (var item in request.product_requests)
                {
                    var product = await db.Product_storage.Where(i => i.id_product == item.id_product).SingleOrDefaultAsync();


                    if (product == null)
                    {
                        return RedirectToAction("ConfirmDetails" + "/" + id, "Requests");

                    } 
                    if (product.count < item.count)
                    {
                        return RedirectToAction("ConfirmDetails" + "/" + id, "Requests");
                    } 
                }

                foreach (var item in request.product_requests)
                {
                    var product =  await db.Product_storage.Where(i => i.id_product == item.id_product).SingleOrDefaultAsync();

                    product.count -= item.count;

                    db.Entry(product).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                }    

                request_db.date_confirm = DateTime.Now;

                request_db.status = 2;

                request_db.cost_request = await CostRequest(id);

                db.Entry(request_db).State = EntityState.Modified;
                await db.SaveChangesAsync();

                return RedirectToAction("Details" + "/" + id, "Requests");
            }
            else
            {
                return RedirectToAction("ConfirmDetails" + "/" + id, "Requests");
            }

        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<ActionResult> ConfirmShip(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Request request = await db.Requests.Include(r => r.product_requests).Include(r => r.customer).SingleOrDefaultAsync(r => r.id_request == id);
            if (request == null)
            {
                return HttpNotFound();
            }

            return View(request);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult> ConfirmShipPost(int? id)
        {

            var request = await db.Requests.FindAsync(id);

            request.status = 3;

            db.Entry(request).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return RedirectToAction("Details" + "/" + id, "Requests");

        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<ActionResult> ConfirmDelivery(int? id)
        {
            ViewBag.date = DateTime.Now;

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Request request = await db.Requests.Include(r => r.product_requests).Include(r => r.customer).SingleOrDefaultAsync(r => r.id_request == id);
            if (request == null)
            {
                return HttpNotFound();
            }

            return View(request);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult> ConfirmDeliveryPost(int? id)
        {

            var request = await db.Requests.FindAsync(id);

            request.date_delivery = DateTime.Now;

            request.status = 4;

            db.Entry(request).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return RedirectToAction("Details" + "/" + id, "Requests");

        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<ActionResult> CancelDetails(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Request request = await db.Requests.Include(r => r.product_requests).Include(r => r.customer).SingleOrDefaultAsync(r => r.id_request == id);
            if (request == null)
            {
                return HttpNotFound();
            }

            return View(request);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult> СonfirmCancel(int? id)
        {
            var request_db = await db.Requests.Include(r => r.product_requests).SingleOrDefaultAsync(r => r.id_request == id);

            var request = await db.Requests.FindAsync(id);

            if (request.status == 2)
            {
                foreach (var item in request_db.product_requests)
                {
                    var product = await db.Product_storage.Where(i => i.id_product == item.id_product).SingleOrDefaultAsync();

                    product.count += item.count;

                    db.Entry(product).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                }
            }
                request.status = 5;

                db.Entry(request).State = EntityState.Modified;
                await db.SaveChangesAsync();
            

            return RedirectToAction("Details" + "/" + id, "Requests");

        }

        [HttpPost]
        [Route("api/[controller]")]
        [Authorize(Roles = "admin,user")]
        public async Task<string> CancelJson(RequestCancel model)
        {
            var request_db = await db.Requests.Include(r => r.product_requests).SingleOrDefaultAsync(r => r.id_request == model.id_request);

            var request = await db.Requests.FindAsync(model.id_request);

            if (request.status == 3 || request.status == 4 || request.status == 5) return "Отмена невозможна";

            if (request.status == 2)
            {
                foreach (var item in request_db.product_requests)
                {
                    var product = await db.Product_storage.Where(i => i.id_product == item.id_product).SingleOrDefaultAsync();

                    product.count += item.count;

                    db.Entry(product).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                }
            }

                request.status = 5;

                db.Entry(request).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return "Заказ отменён";

        }

        [Authorize(Roles = "admin")]
        public async Task<float> CostRequest(int? id)
        {
            var request = await db.Requests.Include(r => r.product_requests).SingleOrDefaultAsync(r => r.id_request == id);

            var type = await db.Types.SingleOrDefaultAsync(i => i.id_type_delivery == request.id_type_delivery);

            float cost = 0;

            foreach (var item in request.product_requests)
            {
                var product = await db.Products.SingleOrDefaultAsync(i => i.id_product == item.id_product);
                cost += (product.cost_product * item.count);

                if (item == request.product_requests.Last())
                {
                    cost += type.cost_type_delivery;
                    return cost;
                }
            }
            return 0;
        }

        [HttpPost]
        [Route("api/[controller]")]
        [Authorize(Roles = "admin,user")]
        public async Task<int> ProductCount(Product_count model)
        {
            var product = await db.Product_storage.SingleOrDefaultAsync(i => i.id_product == model.id_product);

            if (product == null) return 0;

            else return product.count;

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
