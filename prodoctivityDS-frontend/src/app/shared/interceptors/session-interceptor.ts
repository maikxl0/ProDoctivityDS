import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { tap } from 'rxjs/operators';

export const sessionInterceptor: HttpInterceptorFn = (req, next) => {
  const sessionIdKey = 'X-Session-Id';
  const storageKey = 'sessionId';

  let request = req;
  const sessionId = localStorage.getItem(storageKey);
  if (sessionId) {
    request = req.clone({
      headers: req.headers.set(sessionIdKey, sessionId)
    });
  }

  return next(request).pipe(
    tap(event => {
      if (event instanceof HttpResponse) {
        const newSessionId = event.headers.get(sessionIdKey);
        if (newSessionId) {
          localStorage.setItem(storageKey, newSessionId);
        }
      }
    })
  );
};