import urllib.request,urllib.parse,http.cookiejar,re,html,json
from datetime import date,timedelta
from pathlib import Path
BASE='http://localhost:5080'
client=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
results=[]
def get(path):
 with client.open(BASE+path) as r:return html.unescape(r.read().decode())
def post(path,data):
 page=get(path); token=re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',page).group(1)
 data['__RequestVerificationToken']=token
 with client.open(BASE+path,urllib.parse.urlencode(data).encode()) as r:return html.unescape(r.read().decode()),r.url
def check(name,condition):
 if not condition:raise AssertionError(name)
 results.append(name);print('OK',name)
def d(n):return (date.today()+timedelta(days=n)).isoformat()
def rows(text):return re.findall(r'<tr>(.*?)</tr>',text,re.S)
def room(text,num):return any(f'<td>{num}</td>' in row for row in rows(text))
index=get('/Habitaciones');check('8 habitaciones activas y 204 excluida',len(rows(index))==9 and not room(index,'204'))
filtered=get('/Habitaciones?tipo=4');check('Filtro Suite',room(filtered,'301') and room(filtered,'302') and not room(filtered,'101'))
check('Detalle con tipo', 'Simple' in get('/Habitaciones/Details/1'))
page,url=post('/Habitaciones/Create',dict(Numero='101',Piso=1,IdTipoHabitacion=1));check('Número duplicado con error en formulario','El número ya existe' in page and '/Create' in url)
page,url=post('/Habitaciones/Create',dict(Numero='TST910',Piso=0,IdTipoHabitacion=1));check('Piso inválido','El piso debe ser' in page)
page,url=post('/Habitaciones/Create',dict(Numero='TST910',Piso=9,IdTipoHabitacion=4));check('Alta y Post Redirect Get',room(page,'TST910') and '/Create' not in url)
row=next(r for r in rows(page) if '<td>TST910</td>' in r); hid=int(re.search(r'/Habitaciones/Edit/(\d+)',row).group(1))
page,url=post(f'/Habitaciones/Edit/{hid}',dict(IdHabitacion=hid,Numero='TST911',Piso=8,IdTipoHabitacion=2));check('Edición persistida',room(page,'TST911'))
a=get('/Reservas/Disponibles?ingreso='+d(1)+'&salida='+d(4));check('Disponibilidad excluye cruces',not room(a,'101') and room(a,'201'))
a=get('/Reservas/Disponibles?ingreso='+d(4)+'&salida='+d(6));check('Límite contiguo y cancelada no bloquean',room(a,'101'))
a=get('/Reservas/Disponibles?ingreso='+d(3)+'&salida='+d(3));check('Disponibilidad rechaza salida igual al ingreso','La salida debe ser posterior' in a)
form=get('/Reservas/Create');check('Listas solo de activos y Observaciones', 'Villanueva Paz' not in form and '204 —' not in form and 'name="Observaciones"' in form)
def reserve(**extra):
 data=dict(IdHuesped=1,IdHabitacion=1,FechaIngreso=d(30),FechaSalida=d(32),Observaciones='Prueba funcional del laboratorio')
 data.update(extra);return post('/Reservas/Create',data)
a,_=reserve(FechaIngreso=d(-1));check('Ingreso anterior a hoy','El ingreso no puede ser anterior' in a)
a,_=reserve(FechaSalida=d(30));check('Salida debe superar ingreso','La salida debe ser posterior' in a)
a,_=reserve(FechaIngreso=d(1),FechaSalida=d(3));check('Reserva cruzada rechazada','ya tiene una reserva' in a)
a,_=reserve(IdHuesped=7);check('Huésped inactivo rechazado','Selecciona un huésped activo' in a)
a,_=reserve(IdHabitacion=7);check('Habitación inactiva rechazada','Selecciona una habitación activa' in a)
a,_=reserve(Observaciones='x'*201);check('Observaciones máximo 200','Máximo 200 caracteres' in a)
a,url=reserve();check('Reserva Pendiente con observaciones y total', '/Create' not in url and 'Prueba funcional del laboratorio' in a and 'S/ 240.00' in a)
row=next(r for r in rows(a) if 'Prueba funcional del laboratorio' in r); rid=int(re.search(r'<td>(\d+)</td>',row).group(1));check('Estado inicial Pendiente','Pendiente' in row)
a=get('/Reservas/Disponibles?ingreso='+d(30)+'&salida='+d(32));check('Alta bloquea disponibilidad',not room(a,'101'))
# Token emitido en Index por los formularios de cancelación.
def cancel(rid):
 page=get('/Reservas');token=re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',page).group(1)
 with client.open(BASE+f'/Reservas/Cancelar/{rid}',urllib.parse.urlencode({'__RequestVerificationToken':token}).encode()) as r:return html.unescape(r.read().decode())
a=cancel(rid);row=next(r for r in rows(a) if 'Prueba funcional del laboratorio' in r);check('Cancelar cambia estado y conserva reserva','Cancelada' in row)
a=get('/Reservas/Disponibles?ingreso='+d(30)+'&salida='+d(32));check('Cancelación libera habitación',room(a,'101'))
a=cancel(6);check('Finalizada no se puede cancelar','Solo puedes cancelar' in a)
a,url=post(f'/Habitaciones/Delete/{hid}',{});check('Baja lógica desaparece del listado',not room(a,'TST911'))
a,_=post('/Habitaciones/Create',dict(Numero='TST911',Piso=8,IdTipoHabitacion=2));check('Baja conserva número único','El número ya existe' in a)
try:
 client.open(BASE+'/Reservas/Cancelar/1',b'')
 check('Protección antifalsificación',False)
except urllib.error.HTTPError as e:check('Protección antifalsificación',e.code==400)
Path('Hotel.Web/evidencias/verificacion.json').write_text(json.dumps({'fecha':date.today().isoformat(),'resultado':'correcto','casos':results},ensure_ascii=False,indent=2),encoding='utf-8')
