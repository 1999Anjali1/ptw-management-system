import { HttpErrorResponse } from '@angular/common/http';
import { errorMessages } from './api.services';

describe('errorMessages', () => {
  it('returns PTW rule errors from the API', () => {
    const err = new HttpErrorResponse({
      status: 422,
      error: { title: 'Request blocked by PTW rules', errors: ['HotWork requires a gas test before work can start.'] },
    });
    expect(errorMessages(err)).toEqual(['HotWork requires a gas test before work can start.']);
  });

  it('flattens ASP.NET model validation errors', () => {
    const err = new HttpErrorResponse({
      status: 400,
      error: { errors: { Title: ['The Title field is required.'], Location: ['The Location field is required.'] } },
    });
    expect(errorMessages(err)).toEqual(['The Title field is required.', 'The Location field is required.']);
  });

  it('explains when the API is not reachable', () => {
    const err = new HttpErrorResponse({ status: 0 });
    expect(errorMessages(err)[0]).toContain('Cannot reach the API');
  });
});
