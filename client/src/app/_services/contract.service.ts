
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Contract } from '../_model/contract';
import { map, Observable } from 'rxjs';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ContractService {
  contracts: Contract[] = [];
  baseUrl3 = environment.apiUrl3;

  private contractData!: Contract;

  constructor(private http: HttpClient) {
    this.getContracts().subscribe(data => {
      this.contracts = data;
    });
  }

  setContractData(data: Contract) {
    this.contractData = data;
  }

  getContractData(): Contract {
    return this.contractData;
  }

  getContracts(): Observable<Contract[]> {
    return this.http.get<Contract[]>(this.baseUrl3);
  }

  getContract(id: string): Observable<Contract> {
    return this.http.get<Contract>(`${this.baseUrl3}${id}`);
  }

  AddContract(contract: Contract): Observable<Contract> {
      // ✅ Gọi đúng endpoint add-contract
      return this.http.post<Contract>(`${this.baseUrl3}/add-contract`, contract);
    }

    UpdateContract(id: number, contract: Contract): Observable<void> {
      return this.http.put<void>(`${this.baseUrl3}/${id}`, contract);
    }

    DeleteContract(id: number): Observable<void> {
      return this.http.delete<void>(`${this.baseUrl3}/delete-contract/${id}`);
    }

  SearchContract(employeeName: string): Observable<Contract[]> {
    return this.http.get<Contract[]>(`${this.baseUrl3}?employeeName=${employeeName}`).pipe(
      map((response: Contract[]) => response)
    );
  }
}
