#define NOMINMAX 
#define COM_NO_WINDOWS_SECTIONS 
// Добавьте макрос для полного отключения Windows-заголовков
#define WIN32_LEAN_AND_MEAN 
#define NOGDI 
#define NOUSER 
#define WIN32_EXTRA_LEAN 
#define _WIN32_WINNT 0x0501 // Или любая другая минимальная версия

// ЭТО КРИТИЧЕСКИ ВАЖНОЕ ДОПОЛНЕНИЕ:
// Мы определяем COM-интерфейс IServiceProvider как управляемый 
// тип, чтобы заглушить конфликтное определение из servprov.h
#ifdef __cplusplus_cli

// Заглушка, предотвращающая повторное определение IServiceProvider
// ИЛИ 
// Мы можем попытаться изолировать проблемный код:

#ifndef __IServiceProvider_INTERFACE_DEFINED__
// Если IServiceProvider еще не определен (что и вызывает ошибку C2371),
// то мы его определяем как управляемый класс, чтобы остановить 
// включение системного определения.
ref class IServiceProvider;
#define __IServiceProvider_INTERFACE_DEFINED__
#endif // __IServiceProvider_INTERFACE_DEFINED__

#endif // __cplusplus_cli